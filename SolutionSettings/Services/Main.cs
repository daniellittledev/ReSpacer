using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Enexure.SolutionSettings.ReactiveExtensions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Enexure.SolutionSettings.Services
{
	class Main
	{

		#region Settings Paths
		
		private string GetGlobalSettingsPath()
		{
			return Path.Combine(userDataPath, settingsFileName);
		}

		private string GetSolutionSettingsPath(Solution solution)
		{
			if (solution == null) throw new ArgumentNullException("solution");

			var solutionPath = Path.GetDirectoryName(solution.FullName);
			
			if (solutionPath == null) throw new Exception("SolutionPath cannot be null");
			return Path.Combine(solutionPath, settingsFileName);
		}

		private string GetSolutionSettingsPath()
		{
			return GetSolutionSettingsPath(environment.Solution);
		}

		private string GetCurrentSettingsPath()
		{
			if (environment.Solution.IsOpen) {
				var path = GetSolutionSettingsPath(environment.Solution);

				if (File.Exists(path)) {
					return path;
				}
			}

			return GetGlobalSettingsPath();
		}

		#endregion

		private readonly string settingsFileName = "text.settings.json";

		private readonly DTE environment;
		private readonly RegistryKey applicationRegistryRoot;
		private readonly string userDataPath; 

		private readonly VisualStudioMenuManager visualStudioMenuManager;
		private readonly VisualStudioSettingsManager visualStudioSettingsManager;

		//private SettingsFileWatcher settings;

		public Main(DTE environment, RegistryKey applicationRegistryRoot, string userDataPath, OleMenuCommandService menuCommandService)
		{
			this.environment = environment;
			this.applicationRegistryRoot = applicationRegistryRoot;
			this.userDataPath = userDataPath;

			this.visualStudioMenuManager = new VisualStudioMenuManager(menuCommandService);
			this.visualStudioSettingsManager = new VisualStudioSettingsManager(environment);
		}

		public async void Run()
		{
			WireUpEvents();
		}

		private async Task EnsureGlobalSettingsFile()
		{
			/* ensure global settings */
			var settingsPath = GetGlobalSettingsPath();
			if (!File.Exists(settingsPath)) {
				var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
				await SettingsPersister.SaveAsync(settingsPath, settings);
			}
		}

		private void WireUpEvents()
		{
			var watcher = null as SettingsFileWatcher;
			var settingsPath = null as string;

			var switchWatcher = (Action<string>)(newSettingsPath => {

				if (settingsPath != newSettingsPath) {
					if (watcher != null) {
						watcher.Dispose();
					}
					watcher = new SettingsFileWatcher(newSettingsPath);
				}
			});

			var vsReady = VisualStudioReady()
				.ObserveOn(TaskPoolScheduler.Default);
				//.Publish()
				//.RefCount();

			vsReady
				.Subscribe(_ => EnsureGlobalSettingsFile().Wait());

			var solutionSettingsAdding = SolutionSettingsAdding();

			solutionSettingsAdding
				.Subscribe(_ => {
					/* add the project settings file */

					// Always solution settings
					settingsPath = GetSolutionSettingsPath();

					if (!File.Exists(settingsPath)) {
						var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
						SettingsPersister.SaveAsync(settingsPath, settings).Wait();

						// Recreate watcher
						switchWatcher(settingsPath);
					}
				});

			EnvironmentSettingsChanged()
				.Throttle(TimeSpan.FromMilliseconds(300))
				.ObserveOn(TaskPoolScheduler.Default)
				.Subscribe(x => {
					// Current settings
					watcher.Pause();
					var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
					SettingsPersister.SaveAsync(settingsPath, settings).Wait();
					watcher.Resume();
				});

			var solutionOpened = SolutionOpened();
			var solutionClosed = SolutionClosed();

			// Ignore closed if there is an opened after it.
			var openedOrClosedSolution = Observable
				.Merge(solutionOpened, solutionClosed)
				.Throttle(TimeSpan.FromSeconds(0.5));

			var settingsNeedsReload = Observable.Merge(vsReady, openedOrClosedSolution);

			settingsNeedsReload
				.ObserveOn(TaskPoolScheduler.Default)
				.Subscribe(_ => {
					/* reload settings */

					// Current Settings
					var newSettingsPath = GetCurrentSettingsPath();

					var settings = SettingsPersister.LoadAsync(newSettingsPath).Result;
					SettingApplier.Apply(environment, settings);

					// Recreate watcher
					switchWatcher(newSettingsPath);
				});

			Debug.WriteLine("Events wired up!");
		}

		private IObservable<Unit> SolutionSettingsAdding()
		{
			return Observable.FromEventPattern(
				ev => visualStudioMenuManager.OnAddSolutionSettings += ev,
				ev => visualStudioMenuManager.OnAddSolutionSettings -= ev)
				.Select(x => Unit.Default);
		}

		private IObservable<Unit> EnvironmentSettingsChanged()
		{
			return Observable.FromEventPattern(
				ev => visualStudioSettingsManager.OnSettingsUpdated += ev,
				ev => visualStudioSettingsManager.OnSettingsUpdated -= ev)
				.Select(x => Unit.Default);
		}

		private IObservable<Unit> VisualStudioReady()
		{
			return Observable.Return(Unit.Default);
		}

		private IObservable<Unit> SolutionOpened()
		{
			var solution = environment.Events.SolutionEvents;
			return Observable.FromEvent<_dispSolutionEvents_OpenedEventHandler, Unit>(
				c => () => c(Unit.Default),
				x => solution.Opened += x,
				x => solution.Opened -= x);
		}

		private IObservable<Unit> SolutionClosed()
		{
			var solution = environment.Events.SolutionEvents;
			return Observable.FromEvent<_dispSolutionEvents_AfterClosingEventHandler, Unit>(
				c => () => c(Unit.Default), 
				x => solution.AfterClosing += x,
				x => solution.AfterClosing -= x);
		}

	}
}
