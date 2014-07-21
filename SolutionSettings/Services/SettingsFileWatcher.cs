using System;
using System.IO;
using System.Reactive.Linq;

namespace Enexure.SolutionSettings.Services
{
	class SettingsFileWatcher : IDisposable
	{
		private readonly FileSystemWatcher watcher;

		public IObservable<FileSystemEventArgs> OnSettingsFileCreated;
		public IObservable<FileSystemEventArgs> OnSettingsFileChanged;
		public IObservable<FileSystemEventArgs> OnSettingsFileDeleted;

		public SettingsFileWatcher(string fileName)
		{
			watcher = new FileSystemWatcher {
				Filter = fileName,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
			};

			OnSettingsFileCreated = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
				c => (a, b) => c(b),
				x => watcher.Created += x,
				x => watcher.Created -= x);

			OnSettingsFileChanged = Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
				c => (a, b) => c(b),
				x => watcher.Changed += x,
				x => watcher.Changed -= x);

			OnSettingsFileDeleted = Observable.Merge(
				Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(
					c => (a, b) => c(b),
					x => watcher.Deleted += x,
					x => watcher.Deleted -= x),
				Observable.FromEvent<RenamedEventHandler, RenamedEventArgs>(
					c => (a, b) => c(b),
					x => watcher.Renamed += x,
					x => watcher.Renamed -= x)
					.Select(x => new FileSystemEventArgs(WatcherChangeTypes.Deleted, x.OldFullPath, x.Name)));
		}

		public void Dispose()
		{
			watcher.Dispose();
		}

		public void Pause()
		{
			watcher.EnableRaisingEvents = false;
		}

		public void SwitchTo(string settingsPath)
		{
			watcher.Path = Path.GetDirectoryName(settingsPath);
			watcher.EnableRaisingEvents = true;
		}

		public void Resume()
		{
			watcher.EnableRaisingEvents = true;
		}
	}
}
