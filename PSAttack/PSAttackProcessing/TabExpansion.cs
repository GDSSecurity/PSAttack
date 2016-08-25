using PSAttack.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PSAttack.PSAttackProcessing
{
    class TabExpansion
    {
        public static AttackState Process(AttackState attackState)
        {
            if (attackState.loopType == null)
            {
                attackState.cmdComponents = dislayCmdComponents(attackState);
            
                // route to appropriate autcomplete handler
                DisplayCmdComponent cmdSeed = attackState.cmdComponents[attackState.cmdComponentsIndex];
                attackState.loopType = cmdSeed.Type;
                switch (cmdSeed.Type)
                {
                    case "param":
                        attackState = paramAutoComplete(attackState);
                        break;
                    case "variable":
                        attackState = variableAutoComplete(attackState);
                        break;
                    case "path":
                        attackState = pathAutoComplete(attackState);
                        break;
                    default:
                        attackState = cmdAutoComplete(attackState);
                        break;
                }
            }

            // If we're already in an autocomplete loop, increment loopPos appropriately
            else if (attackState.loopType != null)
            {
                if (attackState.keyInfo.Modifiers == ConsoleModifiers.Shift)
                {
                    attackState.loopPos -= 1;
                    // loop around if we're at the beginning
                    if (attackState.loopPos < 0)
                    {
                        attackState.loopPos = attackState.results.Count - 1;
                    }
                }
                else
                {
                    attackState.loopPos += 1;
                    // loop around if we reach the end
                    if (attackState.loopPos >= attackState.results.Count)
                    {
                        attackState.loopPos = 0;
                    }
                }
            }

            // if we have results, format them and return them
            if (attackState.results.Count > 0)
            {
                string seperator = " ";
                string result;
                switch (attackState.loopType)
                {
                    case "param":
                        seperator = " -";
                        result = attackState.results[attackState.loopPos].ToString();
                        break;
                    case "variable":
                        seperator = " $";
                        result = attackState.results[attackState.loopPos].Members["Name"].Value.ToString();
                        break;
                    case "path":
                        seperator = " ";
                        result = "\"" + attackState.results[attackState.loopPos].Members["FullName"].Value.ToString() + "\"";
                        break;
                    default:
                        result = attackState.results[attackState.loopPos].BaseObject.ToString();
                        break;
                }
                // reconstruct display cmd from components
                string completedCmd = "";
                int i = 0;
                int cursorPos = attackState.promptLength;
                while (i < attackState.cmdComponents.Count())
                {
                    if (i == attackState.cmdComponentsIndex)
                    {
                        completedCmd += seperator + result;
                        cursorPos += completedCmd.TrimStart().Length;
                    }
                    else
                    {
                        completedCmd += attackState.cmdComponents[i].Contents;
                    }
                    i++;
                }
                attackState.displayCmd = completedCmd.TrimStart();
                attackState.cursorPos = cursorPos;
            }
            return attackState;
        }

        // This function is used to identify chunks of autocomplete text to determine if it's a variable, path, cmdlet, etc
        // May eventually have to move this to regex to make matches more betterer.
        static String seedIdentification(string seed)
        {
            string seedType = "cmd";
            if (seed.Contains(" -"))
            {
                seedType = "param";
            }
            else if (seed.Contains("$"))
            {
                seedType = "variable";
            }
            else if (seed.Contains("\\") || seed.Contains(":"))
            {
                seedType = "path";
            }
            // This causes an issue and I can't remember why I added this.. leaving it commented 
            // for now in case I need to come back to it (2016/08/21)
            //else if (seed.Length < 4 || seed.First() == ' ')
            //{
            //    seedType = "unknown";
            //}
            return seedType;
        }

        // This function splits text on the command line up and identifies each component
        static List<DisplayCmdComponent> dislayCmdComponents(AttackState attackState)
        {
            List<DisplayCmdComponent> results = new List<DisplayCmdComponent>();
            String[] displayCmdItemList = Regex.Split(attackState.displayCmd, @"(?=[\s])");
            int index = 0;
            int cmdLength = attackState.promptLength + 1;
            foreach (string item in displayCmdItemList)
            {
                string itemType = seedIdentification(item);
                DisplayCmdComponent itemSeed = new DisplayCmdComponent();
                itemSeed.Index = index;
                itemSeed.Contents = item;
                itemSeed.Type = itemType;
                cmdLength += item.Length;
                if ((cmdLength > attackState.cursorPos) && (attackState.cmdComponentsIndex == -1))
                {
                    attackState.cmdComponentsIndex = index;
                }
                if (itemType == "path" || itemType == "unknown")
                {
                    if (results.Last().Type == "path")
                    {
                        results.Last().Contents +=  itemSeed.Contents;
                        
                    }
                    else
                    {
                        results.Add(itemSeed);
                        index++;
                    }
                }
                else
                {
                    results.Add(itemSeed);
                    index++;
                }
            }
            return results;

        }
        // PARAMETER AUTOCOMPLETE
        static AttackState paramAutoComplete(AttackState attackState)
        {
            int index = attackState.cmdComponentsIndex;
            string paramSeed = attackState.cmdComponents[index].Contents.Replace(" -", "");
            string result = ""; 
            while (result != "cmd")
            {
                index -= 1;
                result = attackState.cmdComponents[index].Type;
            }
            string paramCmd = attackState.cmdComponents[index].Contents;
            attackState.cmd = "(Get-Command " + paramCmd + ").Parameters.Keys | Where{$_ -like '" + paramSeed + "*'}";
            attackState = Processing.PSExec(attackState);
            return attackState;
        }

        // VARIABLE AUTOCOMPLETE
        static AttackState variableAutoComplete(AttackState attackState)
        {
            string variableSeed = attackState.cmdComponents[attackState.cmdComponentsIndex].Contents.Replace("$", "");
            attackState.cmd = "Get-Variable " + variableSeed + "*";
            attackState = Processing.PSExec(attackState);
            return attackState;
        }

        // PATH AUTOCOMPLETE
        static AttackState pathAutoComplete(AttackState attackState)
        {
            string pathSeed = attackState.cmdComponents[attackState.cmdComponentsIndex].Contents.Replace("\"","");
            attackState.cmd = "Get-ChildItem \"" + pathSeed.Trim() + "*\"";
            Console.WriteLine(attackState.cmd);
            attackState = Processing.PSExec(attackState);
            return attackState;
        }
                
        // COMMAND AUTOCOMPLETE
        static AttackState cmdAutoComplete(AttackState attackState)
        {
            attackState.cmd = "Get-Command " + attackState.cmdComponents[attackState.cmdComponentsIndex].Contents + "*";
            attackState = Processing.PSExec(attackState);
            return attackState;
        }

    }
}
