using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;

namespace Enexure.SolutionSettings.Services
{
	class VisualStudioSettingsManager
	{
		private readonly DTE environment;

		static readonly VSConstants.VSStd97CmdID[] optionsCommands =
		{
			VSConstants.VSStd97CmdID.ToolsOptions,
			VSConstants.VSStd97CmdID.DebugOptions,
			VSConstants.VSStd97CmdID.CustomizeKeyboard
		};

		public event EventHandler OnSettingsUpdated;

		public VisualStudioSettingsManager(DTE environment)
		{
			this.environment = environment;

			foreach (var optionCmdId in optionsCommands) {
				AddCommandEventHandler(VSConstants.GUID_VSStandardCommandSet97, optionCmdId, ToolsOptionsCommand_AfterExecute);
			}
		}

		// Necessary to prevent event objects from being GC'd.
		// See http://stackoverflow.com/a/13581371/34397
		private readonly List<CommandEvents> commandEventHandlers = new List<CommandEvents>();

		private void AddCommandEventHandler(Guid group, VSConstants.VSStd97CmdID cmdId, _dispCommandEvents_AfterExecuteEventHandler handler)
		{
			var commandEvents = environment.Events.CommandEvents[group.ToString("B"), (int)cmdId];
			commandEvents.AfterExecute += handler;

			commandEventHandlers.Add(commandEvents);
		}

		private void ToolsOptionsCommand_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
		{
			if (OnSettingsUpdated != null) {
				OnSettingsUpdated(this, new EventArgs());
			}
		}
	}
}
