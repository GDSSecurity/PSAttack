using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Security.Principal;
using System.Reflection;
using System.Management.Automation.Runspaces;
using PSAttack.PSAttackProcessing;
using PSAttack.Utils;
using PSAttack.PSAttackShell;

namespace PSAttack
{
    class Program
    {
        static AttackState PSInit()
        {
            // Display Loading Message
            Console.ForegroundColor = PSColors.logoText;
            Random random = new Random();
            int pspLogoInt = random.Next(Strings.psaLogos.Count);
            Console.WriteLine(Strings.psaLogos[pspLogoInt]);
            Console.WriteLine("PS>Attack is loading...");

            // create attackState
            AttackState attackState = new AttackState();
            attackState.cursorPos = Display.createPrompt(attackState).Length;

            // Decrypt modules
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            foreach (string resource in resources)
            {
                if (resource.Contains(".enc"))
                {
                    string fileName = resource.Replace("PSAttack.Modules.","").Replace(".ps1.enc","");
                    string decFilename = CryptoUtils.DecryptString(fileName);
                    Console.ForegroundColor = PSColors.loadingText;
                    Console.WriteLine("Decrypting: " + decFilename);
                    Stream moduleStream = assembly.GetManifestResourceStream(resource);
                    PSAUtils.ImportModules(attackState, moduleStream);
                }
            }
            // Setup PS env
            attackState.cmd = "set-executionpolicy bypass -Scope process -Force";
            Processing.PSExec(attackState);

            // check for admin 
            Boolean isAdmin = false;
            Boolean debugProc = false;
            if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                isAdmin = true;
                try
                {
                    System.Diagnostics.Process.EnterDebugMode();
                    debugProc = true;
                }
                catch
                {
                    Console.Write("Could not grab debug rights for process.");
                }
            }
            
            // Setup Console
            Console.Title = Strings.windowTitle;
            Console.BufferHeight = Int16.MaxValue - 10;
            Console.BackgroundColor = PSColors.background;
            Console.TreatControlCAsInput = true;
            Console.Clear();

            // get build info
            string buildString;
            string attackDate = new StreamReader(assembly.GetManifestResourceStream("PSAttack.Resources.attackDate.txt")).ReadToEnd();
            Boolean builtWithBuildTool = true;
            if (attackDate.Length > 12)
            {                
                buildString = "It was custom made by the PS>Attack Build Tool on " + attackDate + "\n"; 
            }
            else
            {
                string buildDate = new StreamReader(assembly.GetManifestResourceStream("PSAttack.Resources.BuildDate.txt")).ReadToEnd();
                buildString = "It was built on " + buildDate + "\nIf you'd like a version of PS>Attack thats even harder for AV \nto detect checkout http://github.com/jaredhaight/PSAttackBuildTool \n";
                builtWithBuildTool = false;
            }

            // Figure out if we're 32 or 64bit
            string arch = "64bit";
            if (IntPtr.Size == 4)
            {
                arch = "32bit";
            }

            // setup debug variable
            String debugCmd = "$debug = @{'psaVersion'='" + Strings.version + "';'osVersion'='" + Environment.OSVersion.ToString() + "';'.NET'='"
                + System.Environment.Version + "';'isAdmin'='"+ isAdmin + "';'builtWithBuildTool'='" + builtWithBuildTool.ToString() +"';'debugRights'='"
                + debugProc + "';'arch'='" + arch + "'}";
            attackState.cmd = debugCmd;
            Processing.PSExec(attackState);

            // print intro
            Console.ForegroundColor = PSColors.introText;
            Console.WriteLine(Strings.welcomeMessage, Strings.version, buildString);

            // Display Prompt
            attackState.ClearLoop();
            attackState.ClearIO();
            Display.printPrompt(attackState);

            return attackState;
        }

        static void Main(string[] args)
        {
            AttackState attackState = PSInit();
            while (true)
            {
                attackState.keyInfo = Console.ReadKey();
                attackState = Processing.CommandProcessor(attackState);
                Display.Output(attackState);
            }
        }
    }
}