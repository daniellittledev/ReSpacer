using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;

namespace Enexure.SolutionSettings.Services
{
	class SettingsModel : IDisposable
	{
		private readonly DTE environment;
		private string settingsPath;
		private FileSystemWatcher watcher;

		public SettingsModel(DTE environment, string settingsPath)
		{
			this.environment = environment;
			this.settingsPath = settingsPath;

			StartWatcher();
		}

		public void Save()
		{
			// Saves
			throw new NotImplementedException();
		}

		private void StartWatcher()
		{
			Debug.Assert(watcher == null, "watcher == null");

			// Create a new FileSystemWatcher and set its properties.
			watcher = new FileSystemWatcher {
				Path = Path.GetDirectoryName(settingsPath),
				Filter = Path.GetFileName(settingsPath),
				NotifyFilter = NotifyFilters.LastWrite,
			};

			watcher.Changed += async (a, b) => { await ReApplySettingsFile(); };

			// Start
			watcher.EnableRaisingEvents = true;
		}

		async Task ReApplySettingsFile()
		{
			var settings = await SettingsPersister.Load(settingsPath);
			SettingApplier.Apply(environment, settings);
		}

		public void Dispose()
		{
			if (watcher != null) {
				watcher.Dispose();
			}
		}

		internal void Move(string settingsPath)
		{
			this.settingsPath = settingsPath;

			// Takes current settings and moves to new path
			throw new NotImplementedException();

			Save();
		}
	}
}
