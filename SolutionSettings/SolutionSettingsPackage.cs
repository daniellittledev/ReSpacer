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
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidSolutionSettingsPkgString)]
    public sealed class SolutionSettingsPackage : Package
    {
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



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            Run();
        }
        #endregion

        private void Run()
        {
            DTE vsEnvironment = (DTE)GetService(typeof(SDTE));


            Debug.WriteLine(this.UserLocalDataPath);

            
            var propertyPageNames = GetPropertyPages(this.ApplicationRegistryRoot);


            var solutionPath = Path.GetDirectoryName(vsEnvironment.Solution.FullName);
            
            var settingsPath = Path.Combine(solutionPath, "text.settings.json");

            string fileContents = null;
            try {
                fileContents = File.ReadAllText(settingsPath);
            } catch (FileNotFoundException ex) {
                return;
            }

            var settings = JsonConvert.DeserializeObject(fileContents, typeof(ItemSetting[]));
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

        private void ApplySettings(DTE vsEnvironment, IEnumerable<ItemSetting> settings)
        {
            foreach (var setting in settings) {
                Properties propertiesList = vsEnvironment.get_Properties("TextEditor", setting.Name);
                if (null == propertiesList) {
                    // The specified properties collection is not available. 
                    return;
                }

                ChangeSettings(propertiesList, setting.Settings);
            }
        }

        private void ChangeSettings(Properties propertiesList, TabSettings settings)
        {
            //_vsIndentStyle.vsIndentStyleDefault

            Property tabSize = propertiesList.Item("TabSize");
            short oldSize = (short)tabSize.Value;

            string message;
            if (oldSize != 4) {
                tabSize.Value = 4;
                message = string.Format(CultureInfo.CurrentUICulture,
                    "For Basic, the Text Editor had a tab size of {0}" +
                    " and now has a tab size of {1}.", oldSize, tabSize.Value);
            } else {
                message = string.Format(CultureInfo.CurrentUICulture,
                    "For Basic, the Text Editor has a tab size of {0}.", tabSize.Value);
            }

            MessageBox.Show(message, "Text Editor, Basic, Tab Size:",
                MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, 0);
        }

    }
}
