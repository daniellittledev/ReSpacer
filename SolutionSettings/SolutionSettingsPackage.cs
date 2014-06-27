using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Enexure.SolutionSettings.Commands;
using Enexure.SolutionSettings.Settings;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace Enexure.SolutionSettings
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists)]
    // For menus
    [ProvideMenuResource("Menus.ctmenu", 1)] 
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidSolutionSettingsPkgString)]
    public sealed class SolutionSettingsPackage : Package
    {
        private OleMenuCommand addSolutionSettingsCommand; 

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SolutionSettingsPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            try {
                Run();
            } catch (Exception ex) {
                throw;
            }

        }

        private readonly string settingsFileName = "text.settings.json";

        private DTE GetEnvironment()
        {
            return (DTE)GetService(typeof(SDTE));
        }

        private string GetMasterSettingsPath()
        {
            return Path.Combine(this.UserDataPath, @"text.settings.json");
        }

        private string GetSolutionSettingsPath(Solution solution)
        {
            var solutionPath = Path.GetDirectoryName(solution.FullName);

            Debug.Assert(solutionPath != null, "solutionPath != null");
            return Path.Combine(solutionPath, "text.settings.json");
        }

        private void Run()
        {
            OnStartup();

            AddUI();

            // This might have to be moved, could load before there is a solution.
            //OnSolutionOpened();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            var environment = GetEnvironment();

            environment.Events.SolutionEvents.Opened += OnSolutionOpened;
            environment.Events.SolutionEvents.BeforeClosing += OnSolutionClosing;
        }

        private void OnStartup()
        {
            var environment = GetEnvironment();

            var masterSettingsPath = GetMasterSettingsPath();

            if (File.Exists(masterSettingsPath)) {
                LoadSettingsFromFile(environment, masterSettingsPath);
            } else {
                SaveGlobalSettingsToFile(environment, this.ApplicationRegistryRoot, masterSettingsPath);
            }
        }

        // There is no event for this...
        private void OnEnvironmentSettingsChanged()
        {
            var environment = GetEnvironment();

            SaveGlobalSettingsToFile(environment, this.ApplicationRegistryRoot, GetMasterSettingsPath());

            ReapplySolutionSettings();
        }

        private void ReapplySolutionSettings()
        {
            OnSolutionSettingsChanged();
        }

        private void OnSolutionOpened()
        {
            OnSolutionSettingsChanged();

            // Watch Solution Files
            var environment = GetEnvironment();
            WatchSolutionFile(GetSolutionSettingsPath(environment.Solution));
        }

        private FileSystemWatcher watcher;

        private void WatchSolutionFile(string solutionSettingsPath)
        {
            Debug.Assert(watcher == null, "watcher == null");

            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher {
                Path = Path.GetDirectoryName(solutionSettingsPath),
                Filter = Path.GetFileName(solutionSettingsPath),
                NotifyFilter = NotifyFilters.LastWrite,
            };

            watcher.Changed += watcher_Changed;

            // Start
            watcher.EnableRaisingEvents = true;
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            ReapplySolutionSettings();
        }

        private void OnSolutionClosing()
        {
            // Stop Watching Solution Files

            if (watcher != null) {
                watcher.Dispose();
            }
        }

        private void OnSolutionSettingsChanged()
        {
            DTE environment = (DTE)GetService(typeof(SDTE));

            var settingsPath = GetSolutionSettingsPath(environment.Solution);

            LoadSettingsFromFile(environment, settingsPath);
        }

        private void AddUI()
        {
            // Now get the OleCommandService object provided by the MPF; this object is the one 
            // responsible for handling the collection of commands implemented by the package. 
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs) {

                // For each command we have to define its id that is a unique Guid/integer pair. 
                var id = new CommandID(GuidList.guidSolutionSettingsCmdSet, PkgCmdIDList.cmdIdAddSolutionSettings);

                // Now create the OleMenuCommand object for this command.
                var command = new OleMenuCommand(AddSolutionSettingsMenuCommandCallback, id);

                // Add the command to the command service. 
                mcs.AddCommand(command);
            }
        }

        private void AddSolutionSettingsMenuCommandCallback(object sender, EventArgs e)
        {
            var environment = GetEnvironment();
            var settingsPath = GetSolutionSettingsPath(environment.Solution);

            SaveGlobalSettingsToFile(environment, this.ApplicationRegistryRoot, settingsPath);
        }

        private void Write(string text)
        {
            var windowPane = (IVsOutputWindowPane)GetService(typeof(SVsGeneralOutputWindowPane));
            if (null == windowPane) {
                Debug.WriteLine("Failed to get a reference to the Output window General pane");
                return;
            }
            if (Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.OutputString(text))) {
                Debug.WriteLine("Failed to write on the Output window");
            } 
        }

        private static void LoadSettingsFromFile(DTE environment, string settingsPath)
        {
            string fileContents = null;
            try {
                fileContents = File.ReadAllText(settingsPath);
            } catch (FileNotFoundException ex) {
                return;
            }

            var settings = (IEnumerable<ItemSetting>)JsonConvert.DeserializeObject(fileContents, typeof(ItemSetting[]));

            ApplySettings(environment, settings);
        }

        private static void SaveGlobalSettingsToFile(DTE environment, RegistryKey applicationRegistryRoot, string settingsPath)
        {
            SaveSettingsToFile(GenerateGlobalSettings(environment, applicationRegistryRoot), settingsPath);
        }

        private static void SaveSettingsToFile(IReadOnlyCollection<ItemSetting> settings, string settingsPath)
        {
            var data = JsonConvert.SerializeObject(settings);
            File.WriteAllText(settingsPath, data);
        }

        private static IReadOnlyCollection<ItemSetting> GenerateGlobalSettings(DTE environment, RegistryKey applicationRegistryRoot)
        {
            return GetPropertyPages(applicationRegistryRoot)
                .Select(pageName => new ItemSetting() {
                    Name = pageName,
                    Settings = LoadSettings(environment, pageName)
                })
                .Where(x => x.Settings != null)
                .ToList();
        }

        private static IEnumerable<string> GetPropertyPages(RegistryKey rootKey)
        {
            //var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            //var userSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            using (var automationProperties = rootKey.OpenSubKey("AutomationProperties")) {
                if (automationProperties != null) {
                    using (var textEditor = automationProperties.OpenSubKey("TextEditor")) {
                        if (textEditor != null) {
                            foreach (var subkeyName in textEditor.GetSubKeyNames()) {
                                yield return subkeyName;
                            }
                        } else {
                            throw new Exception("The key 'TextEditor' is missing");
                        }
                    }
                } else {
                    throw new Exception("The key 'AutomationProperties' is missing");
                }
            }
        }

        private static TabSettings LoadSettings(DTE environment, string languageName)
        {
            var properties = environment.Properties["TextEditor", languageName];

            if (!HasProperty(properties, "IndentStyle")) {
                return null;
            }

            return new TabSettings() {
                IndentStyle = GetValue<IndentStyle>(properties.Item("IndentStyle")),
                TabSize = GetValue<int>(properties.Item("TabSize")),
                IndentSize = GetValue<int>(properties.Item("IndentSize")),
                InsertTabs = GetValue<bool>(properties.Item("InsertTabs")),
            };
        }

        private static bool HasProperty(Properties properties, string name)
        {
            try {
                properties.Item(name);
                return true;
            } catch (Exception) {
                return false;
            }
            
            /*
            var len = properties.Count + 1;
            for (int i = 1; i < len; i++) {
                if (properties.Item(i).Name == name) {
                    return true;
                }
            }
            return false;
            */
        }

        private static T? GetValue<T>(Property item)
            where T : struct 
        {
            try {
                return (T)Convert.ChangeType(item.Value, typeof(T));
            } catch (Exception) {
                return null;
            }
        }

        private static void SaveSettings(DTE environment, string languageName, TabSettings tabSettings)
        {
            var properties = environment.Properties["TextEditor", languageName];

            UpdatePropertyItem(properties, "IndentStyle", tabSettings.IndentStyle);
            UpdatePropertyItem(properties, "TabSize", tabSettings.TabSize);
            UpdatePropertyItem(properties, "IndentSize", tabSettings.IndentSize);
            UpdatePropertyItem(properties, "InsertTabs", tabSettings.InsertTabs);
        }

        private static void UpdatePropertyItem(Properties properties, string name, object value)
        {
            if (value != null) {
                properties.Item(name).Value = value;
            }
        }

        private static void ApplySettings(DTE environment, IEnumerable<ItemSetting> settings)
        {
            foreach (var setting in settings) {
                SaveSettings(environment, setting.Name, setting.Settings);
            }
        }

    }
}
