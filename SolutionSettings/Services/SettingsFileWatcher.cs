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

			watcher.Changed += watcher_Changed;

			// Start
			watcher.EnableRaisingEvents = true;
		}

		void watcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (OnSettingsFileChanged != null) {
				OnSettingsFileChanged(this, new EventArgs());
			}
		}
		
		public void Dispose()
		{
			watcher.Changed -= watcher_Changed;
			watcher.Dispose();
		}

		public void Pause()
		{
			watcher.EnableRaisingEvents = false;
		}

		public void Resume()
		{
			watcher.EnableRaisingEvents = true;
		}
	}
}
