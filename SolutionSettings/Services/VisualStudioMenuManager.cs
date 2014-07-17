using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enexure.SolutionSettings.Commands;
using Microsoft.VisualStudio.Shell;

namespace Enexure.SolutionSettings.Services
{
	class VisualStudioMenuManager
	{
		private readonly OleMenuCommandService menuCommandService;

		private OleMenuCommand addSolutionSettingsCommand;
		private OleMenuCommand openGlobalSettingsCommand; 

		public VisualStudioMenuManager(OleMenuCommandService menuCommandService)
		{
			this.menuCommandService = menuCommandService;
		}

		public event EventHandler OnOpenGlobalSettings;
		public event EventHandler OnAddSolutionSettings; 

		private void SetupUserInterface()
		{
			if (null != menuCommandService) {
				ConfigureAddSolutionSettingsCommand(menuCommandService);
				//ConfigureOpenGlobalSettingsCommand(menuCommandService);
			}
		}

		private void ConfigureOpenGlobalSettingsCommand(OleMenuCommandService mcs)
		{
			// For each command we have to define its id that is a unique Guid/integer pair. 
			var id = new CommandID(GuidList.guidSolutionSettingsCmdSet, PkgCmdIDList.cmdIdOpenGlobalSolutionSettings);

			// Now create the OleMenuCommand object for this command.
			var command = new OleMenuCommand(OpenGlobalSettingsMenuCommandCallback, id);

			// Add the command to the command service. 
			mcs.AddCommand(command);

			openGlobalSettingsCommand = command;
		}

		private void OpenGlobalSettingsMenuCommandCallback(object sender, EventArgs e)
		{
			if (OnOpenGlobalSettings != null) {
				OnOpenGlobalSettings(this, e);
			}
		}

		private void ConfigureAddSolutionSettingsCommand(OleMenuCommandService mcs)
		{
			// For each command we have to define its id that is a unique Guid/integer pair. 
			var id = new CommandID(GuidList.guidSolutionSettingsCmdSet, PkgCmdIDList.cmdIdAddSolutionSettings);

			// Now create the OleMenuCommand object for this command.
			var command = new OleMenuCommand(AddSolutionSettingsMenuCommandCallback, id);

			// Add the command to the command service. 
			mcs.AddCommand(command);

			addSolutionSettingsCommand = command;
		}

		private void AddSolutionSettingsMenuCommandCallback(object sender, EventArgs e)
		{
			if (OnAddSolutionSettings != null) {
				OnAddSolutionSettings(this, e);
			}
		}
	}
}
