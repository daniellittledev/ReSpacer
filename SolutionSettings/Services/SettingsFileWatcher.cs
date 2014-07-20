using System;
using System.IO;

namespace Enexure.SolutionSettings.Services
{
	class SettingsFileWatcher : IDisposable
	{
		private readonly FileSystemWatcher watcher;
		
		public event EventHandler<string> OnSettingsFileChanged;
		public event EventHandler<string> OnSettingsFileDeleted;

		public SettingsFileWatcher(string fileName)
		{
			watcher = new FileSystemWatcher {
				Filter = fileName,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
			};

			watcher.Renamed += WatcherOnRenamed;
			watcher.Changed += WatcherOnChanged;
			watcher.Deleted += WatcherOnDeleted;
		}

		private void WatcherOnRenamed(object sender, RenamedEventArgs renamedEventArgs)
		{
			if (OnSettingsFileDeleted != null) {
				OnSettingsFileDeleted(this, renamedEventArgs.OldFullPath);
			}
		}

		private void WatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
		{
			if (OnSettingsFileDeleted != null) {
				OnSettingsFileDeleted(this, fileSystemEventArgs.FullPath);
			}
		}

		void WatcherOnChanged(object sender, FileSystemEventArgs e)
		{
			if (OnSettingsFileChanged != null) {
				OnSettingsFileChanged(this, e.FullPath);
			}
		}
		
		public void Dispose()
		{
			watcher.Deleted -= WatcherOnDeleted;
			watcher.Changed -= WatcherOnChanged;
			watcher.Dispose();
		}

		public void Pause()
		{
			watcher.EnableRaisingEvents = false;
		}

		public void SwitchTo(string settingsPath)
		{
			watcher.Path = settingsPath;
			watcher.EnableRaisingEvents = true;
		}

		public void Resume()
		{
			watcher.EnableRaisingEvents = true;
		}
	}
}
