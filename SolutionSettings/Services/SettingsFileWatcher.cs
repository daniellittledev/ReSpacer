using System;
using System.IO;

namespace Enexure.SolutionSettings.Services
{
	class SettingsFileWatcher : IDisposable
	{
		private readonly FileSystemWatcher watcher;
		
		public event EventHandler OnSettingsFileChanged; 

		public SettingsFileWatcher(string settingsPath)
		{
			watcher = new FileSystemWatcher {
				Path = Path.GetDirectoryName(settingsPath),
				Filter = Path.GetFileName(settingsPath),
				NotifyFilter = NotifyFilters.LastWrite,
			};

			watcher.Changed += (a, b) => {
				if (OnSettingsFileChanged != null) {
					OnSettingsFileChanged(this, b);
				}
			};

			// Start
			watcher.EnableRaisingEvents = true;
		}
		
		public void Dispose()
		{
			if (watcher != null) {
				watcher.Dispose();
			}
		}
	}
}
