using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Enexure.SolutionSettings.Commands
{
    class AddSolutionSettingsCommand : OleMenuCommand
    {
        /// <summary> 
        /// This is the function that is called when the user clicks on the menu command. 
        /// </summary> 
        private static void ClickCallback(object sender, EventArgs args) 
        { 
            var cmd = sender as AddSolutionSettingsCommand; 
            if (null != cmd) 
            { 
                // Do action
            } 
        } 
 
        /// <summary> 
        /// Creates a new AddSolutionSettingsCommand object with a specific CommandID and base text. 
        /// </summary> 
        public AddSolutionSettingsCommand(CommandID id, string text) : 
            base(new EventHandler(ClickCallback), id, text) 
        {

        }
 
        /// <summary>
        /// Command text
        /// </summary>
        public override string Text { get; set; }
    }
}
