using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSAttack.PSAttackShell;
using PSAttack.Utils;


namespace PSAttack.PSAttackProcessing
{
    // This item is used to keep track of the various components on the command line
    public class DisplayCmdComponent
    {
        public int Index { get; set; }
        public string Contents { get; set; }
        public string Type { get; set; }
    }

    class AttackState
    {
        // Powershell runsapce and host
        public Runspace runspace { get; set; }
        public PSAttackHost host { get; set; }

        // contents of cmd are what are executed
        public string cmd { get; set; }

        // contents of displayCmd are what are shown on screen as the command
        public string displayCmd { get; set; }
        
        // string to run autocomplete against
        public string autocompleteSeed { get; set; }

        // string to store displayCmd for autocomplete concatenation
        public string displayCmdSeed { get; set; }

        // key that was last pressed
        public ConsoleKeyInfo keyInfo { get; set; }
        
        // we set a loopPos for when we're in a tab-complete loop
        public int loopPos { get; set; }

        // The vertical position of the last prompt printed. Used so we know where to start re-writing commands
        public int promptPos { get; set; }

        // absolute cusor position (not accounting for wrapping in the window)
        public int cursorPos { get; set; }
        
        // loop states
        public string loopType { get; set; }
        
        // ouput is what's print to screen
        public string output { get; set; }
        
        // used for auto-complete loops
        public Collection<PSObject> results { get; set; }

        // set once execution of a command has completed, breaks the while loop in main.
        public bool cmdComplete { get; set; }

        // used to store command history
        public List<string> history { get; set; }

        public int promptLength { get; set; }

        // When PSAttack is built an encrypted CSV is generated containing data that we 
        // don't want to touch disk. That data is stored here as a dict 
        public Dictionary<string,string> decryptedStore { get; set; }

        // used to store list of command components and their types
        public List<DisplayCmdComponent> cmdComponents { get; set; }

        // used to store index of command compnotent being auto-completed.
        public int cmdComponentsIndex { get; set; }

        // returns total length of display cmd + prompt. Used to check for text wrap in 
        // so we know what to do with our cursor
        public int totalDisplayLength()
        {
            return this.promptLength + this.displayCmd.Length;
        }

        public int consoleWrapCount()
        {
            return this.totalDisplayLength() / Console.WindowWidth;
        }

        // return cursor pos ignoring window wrapping
        public int relativeCursorPos()
        {
            int wrapCount = this.consoleWrapCount();
            if (wrapCount > 0)
            {
                return this.cursorPos + Console.WindowWidth* wrapCount;
            }
            return this.cursorPos;
        }

        // return relative cusor pos without prompt
        public int relativeCmdCursorPos()
        {
            List<int> cursorXY = this.getCursorXY();
            return this.cursorPos - this.promptLength;
        }

        // This is used to figure out where the cursor should be placed, accounting for line
        // wraps in the command and where the prompt is
        public List<int> getCursorXY()
        {
            // figure out if we've dropped down a line
            int cursorYDiff = this.cursorPos / Console.WindowWidth;
            int cursorY = this.promptPos + this.cursorPos / Console.WindowWidth;
            int cursorX = this.cursorPos - Console.WindowWidth * cursorYDiff;

            // if X is < 0, set cursor to end of line
            if (cursorX < 0) {
                cursorX = Console.WindowWidth - 1;
            }
            List<int> cursorXY = new List<int>();
            cursorXY.Add(cursorX);
            cursorXY.Add(cursorY);
            return cursorXY;

        }

        // return end of displayCmd accounting for prompt
        public int endOfDisplayCmdPos()
        {
            return this.promptLength + this.displayCmd.Length;
        }

        // clear out cruft from autocomplete loops
        public void ClearLoop()
        {
            this.loopType = null;
            this.results = null;
            this.autocompleteSeed = null;
            this.displayCmdSeed = null;
            this.loopPos = 0;
            this.cmdComponents = null;
            this.cmdComponentsIndex = -1;
        }

        // clear out cruft from working with commands
        public void ClearIO(bool display=false)
        {
            if (display == true)
            {
                this.displayCmd = "";
            }
            this.cmd = "";
            this.keyInfo = new ConsoleKeyInfo();
            this.cmdComplete = false;
            this.output = null;
        }

        public AttackState()
        {
            // init host and runspace
            this.host = new PSAttackHost();
            Runspace runspace = RunspaceFactory.CreateRunspace(this.host);
            runspace.Open();
            this.runspace = runspace;
            this.history = new List<string>();
            this.decryptedStore = new Dictionary<string, string>();
            // hack to keep cmd from being null. others parts of psa don't appreciate that.
            this.cmd = "";
            this.displayCmd = "";
        }
    }
}
