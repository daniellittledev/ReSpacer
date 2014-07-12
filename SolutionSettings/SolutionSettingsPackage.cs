using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Enexure.SolutionSettings.Commands;
using Enexure.SolutionSettings.Services;
using Enexure.SolutionSettings.Settings;
using Enexure.SolutionSettings.Settings.Version1;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
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
	[ProvideAutoLoad(UIContextGuids80.NoSolution)]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists)]
	// For menus
	[ProvideMenuResource("Menus.ctmenu", 1)] 
	// This attribute is used to register the information needed to show this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(GuidList.guidSolutionSettingsPkgString)]
	public sealed class SolutionSettingsPackage : Package
	{
		private Main main;

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
				main = new Main(GetEnvironment(), ApplicationRegistryRoot, UserDataPath, GetMenuCommandService());
				main.Run();

			} catch (Exception ex) {
				throw;
			}
		}

		private DTE GetEnvironment()
		{
			return (DTE)GetService(typeof(SDTE));
		}

		private OleMenuCommandService GetMenuCommandService()
		{
			// Now get the OleCommandService object provided by the MPF; this object is the one 
			// responsible for handling the collection of commands implemented by the package. 
			return GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
		}
	}
}
