using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSAttack.PSAttackShell;
using PSAttack.PSAttackProcessing;


namespace PSAttack.Utils
{
    class Display
    {
        public static string createPrompt(AttackState attackState)
        {
            string prompt = attackState.runspace.SessionStateProxy.Path.CurrentLocation + " #> ";
            attackState.promptLength = prompt.Length;
            return prompt;
        }

        public static void Output(AttackState attackState)
        {
            if (attackState.cmdComplete)
            {
                printPrompt(attackState);
            }
            int currentCusorPos = Console.CursorTop;
            string prompt = createPrompt(attackState);

            // This is where we juggle things to make sure the cursor ends up where 
            // it's expected to be. I'm sure this could be improved on.

            // Clear out typed text after prompt
            Console.SetCursorPosition(prompt.Length, attackState.promptPos);
            Console.Write(new string(' ', Console.WindowWidth));

            // Clear out any lines below the prompt
            int cursorDiff = attackState.consoleWrapCount();
            while (cursorDiff > 0)
            {
                Console.SetCursorPosition(0, attackState.promptPos + cursorDiff);
                Console.Write(new string(' ', Console.WindowWidth));
                cursorDiff -= 1;
            }
            Console.SetCursorPosition(prompt.Length, attackState.promptPos);

            // Re-print the command
            Console.Write(attackState.displayCmd);

            List<int> cursorXY = attackState.getCursorXY();
            Console.SetCursorPosition(cursorXY[0], cursorXY[1]);
        }

        public static void Exception(AttackState attackState, string errorMsg)
        {
            Console.ForegroundColor = PSColors.errorText;
            Console.WriteLine("ERROR: {0}\n", errorMsg);
        }

        public static void printPrompt(AttackState attackState)
        {
            attackState.promptPos = Console.CursorTop;
            string prompt = createPrompt(attackState);
            Console.ForegroundColor = PSColors.prompt;
            Console.Write(prompt);
            Console.ForegroundColor = PSColors.inputText;
            attackState.cursorPos = prompt.Length;
        }
    }
}
