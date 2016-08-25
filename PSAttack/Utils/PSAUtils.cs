using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using PSAttack.PSAttackShell;
using PSAttack.PSAttackProcessing;

namespace PSAttack.Utils
{
    class PSAUtils
    {
        public static void ImportModules(AttackState attackState, Stream moduleStream)
        {
            try
            {
                MemoryStream decMem = CryptoUtils.DecryptFile(moduleStream);
                attackState.cmd = Encoding.Unicode.GetString(decMem.ToArray());
                Processing.PSExec(attackState);
            }
            catch (Exception e)
            {
                ConsoleColor origColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(Strings.moduleLoadError, e.Message);
                Console.ForegroundColor = origColor;
            }
        }
    }
}
