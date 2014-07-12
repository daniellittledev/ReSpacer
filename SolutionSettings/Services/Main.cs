using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Remoting.Proxies;
using System.Text;
using System.Threading.Tasks;
using Enexure.SolutionSettings.Commands;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace Enexure.SolutionSettings.Services
{
	class Main
	{
		private OleMenuCommand addSolutionSettingsCommand; 

		private readonly string settingsFileName = "text.settings.json";

		private readonly DTE environment;
		private readonly RegistryKey applicationRegistryRoot;
		private readonly string userDataPath;
		private readonly OleMenuCommandService menuCommandService;

		private SettingsModel settings;

		public Main(DTE environment, RegistryKey applicationRegistryRoot, string userDataPath, OleMenuCommandService menuCommandService)
		{
			this.environment = environment;
			this.applicationRegistryRoot = applicationRegistryRoot;
			this.userDataPath = userDataPath;
			this.menuCommandService = menuCommandService;
		}

		public void Run()
		{
			OnStartup();

			SetupUserInterface();

			WireUpEvents();
		}
		
		private string GetGlobalSettingsPath()
		{
			return Path.Combine(userDataPath, settingsFileName);
		}

		private string GetSolutionSettingsPath(Solution solution)
		{
			if (solution == null) throw new ArgumentNullException("solution");

			var solutionPath = Path.GetDirectoryName(solution.FullName);
			Debug.Assert(solutionPath != null, "solutionPath != null");
			return Path.Combine(solutionPath, settingsFileName);
		}

		static readonly VSConstants.VSStd97CmdID[] optionsCommands =
		{
			VSConstants.VSStd97CmdID.ToolsOptions,
			VSConstants.VSStd97CmdID.DebugOptions,
			VSConstants.VSStd97CmdID.CustomizeKeyboard
		};

		private void WireUpEvents()
		{
			environment.Events.SolutionEvents.Opened += OnSolutionOpened;
			environment.Events.SolutionEvents.BeforeClosing += OnSolutionClosing;

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
			// After the user changes any options, save them.
			settings.Save();
		}

		private void OnStartup()
		{
			EnsureGlobalSettingsAreSaved();

			// wait to open in case we're just about to open a new solution.
		}

		private void EnsureGlobalSettingsAreSaved()
		{
			var masterSettingsPath = GetMasterSettingsPath();
			throw new NotImplementedException();
		}

		private void OnSolutionOpened()
		{
			addSolutionSettingsCommand.Visible = true;

			var settingsPath = GetSolutionSettingsPath(environment.Solution);

			// wait to open in case we're just about to close the solution.
			settings = new SettingsModel(environment, settingsPath);
		}

		private void OnSolutionClosing()
		{
			// wait to open in case we're just about to open a new solution.
			addSolutionSettingsCommand.Visible = false;
		}

		private void SetupUserInterface()
		{
			if (null != menuCommandService) {

				ConfigureAddSolutionSettingsCommand(menuCommandService);
				ConfigureOpenGlobalSettingsCommand(menuCommandService);
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
		}

		private void OpenGlobalSettingsMenuCommandCallback(object sender, EventArgs e)
		{
			var settingsPath = GetGlobalSettingsPath();
			OpenFile(environment, settingsPath);
		}

		private void ConfigureAddSolutionSettingsCommand(OleMenuCommandService mcs)
		{
			// For each command we have to define its id that is a unique Guid/integer pair. 
			var id = new CommandID(GuidList.guidSolutionSettingsCmdSet, PkgCmdIDList.cmdIdAddSolutionSettings);

			// Now create the OleMenuCommand object for this command.
			var command = new OleMenuCommand(AddSolutionSettingsMenuCommandCallback, id);

			// Add the command to the command service. 
			mcs.AddCommand(command);

			command.Visible = false;

			addSolutionSettingsCommand = command;
		}

		private void AddSolutionSettingsMenuCommandCallback(object sender, EventArgs e)
		{
			var settingsPath = GetSolutionSettingsPath(environment.Solution);

			settings.Move(settingsPath);

			if (!alreadyExists) {
				AddFileToSolution(environment, settingsPath);
				OpenFile(environment, settingsPath);
			}
		}

		private static void AddFileToSolution(DTE environment, string filePath)
		{
			// Select the solution
			var window = environment.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
			var hierarchy = window.Object as UIHierarchy;
			Debug.Assert(hierarchy != null, "hierarchy != null");
			var rootItem = hierarchy.UIHierarchyItems.Item(1);
			rootItem.Select(vsUISelectionType.vsUISelectionTypeSelect);

			environment.ItemOperations.AddExistingItem(filePath);
		}

		private static void OpenFile(DTE environment, string filePath)
		{
			environment.ItemOperations.OpenFile(filePath);
		}
	}
}
