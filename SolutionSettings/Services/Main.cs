using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Enexure.SolutionSettings.IO;
using Enexure.SolutionSettings.ReactiveExtensions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Seq;
using Serilog;
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

		private readonly VisualStudioStatusBar visualStudioStatusBar;
		private readonly VisualStudioMenuManager visualStudioMenuManager;
		private readonly VisualStudioSettingsManager visualStudioSettingsManager;

		//private SettingsFileWatcher settings;

		public Main(DTE environment, RegistryKey applicationRegistryRoot, string userDataPath, OleMenuCommandService menuCommandService, IVsStatusbar statusbar)
		{
			this.environment = environment;
			this.applicationRegistryRoot = applicationRegistryRoot;
			this.userDataPath = userDataPath;

			this.visualStudioStatusBar = new VisualStudioStatusBar(statusbar);
			this.visualStudioMenuManager = new VisualStudioMenuManager(menuCommandService);
			this.visualStudioSettingsManager = new VisualStudioSettingsManager(environment);
		}

		public void Run()
		{
			WireUpEvents();

			//Log.Logger = new LoggerConfiguration()
			//	.WriteTo.Seq("http://localhost:5341")
			//	.CreateLogger();

		}

		private void WireUpEvents()
		{
			var watcher = new SettingsFileWatcher(settingsFileName);

			var vsReady = VisualStudioReady();
			var solutionSettingsAdding = SolutionSettingsAdding();
			var solutionOpened = SolutionOpened();
			var solutionClosed = SolutionClosed();
			var environmentSettingsChanged = EnvironmentSettingsChanged();
			var fileSettingsChanged = SettingsFileChanged(watcher);
			var fileSettingsDeleted = SettingsFileDeleted(watcher);

			var settingsPath = null as string;

			var switchWatcher = (Action<string>)(newSettingsPath => {

				if (settingsPath != newSettingsPath) {

					var globalSettings = GetGlobalSettingsPath();
					visualStudioStatusBar.UpdateStatus(newSettingsPath == globalSettings ? "Loaded global ReSpacer settings" : "Loaded solution ReSpacer settings");

					watcher.SwitchTo(Path.GetDirectoryName(newSettingsPath));
					settingsPath = newSettingsPath;
				}
			});

			vsReady
				.Where(path => !File.Exists(path))
				.Select(path => new { Path = path, Settings = SettingApplier.Extract(environment, applicationRegistryRoot) })
				//.Trace("vsReady - Select")
				.SelectMany(async x => {
					await SettingsPersister.SaveAsync(x.Path, x.Settings);
					return Unit.Default;
				})
				//.Trace("vsReady - SelectMany")
				.Subscribe();

			solutionSettingsAdding
				.Select(_ => GetSolutionSettingsPath())
				.Where(path => !File.Exists(path))
				.SelectMany(async path => {
					
					var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
					await SettingsPersister.SaveAsync(path, settings);

					return path;
				})
				.Subscribe(path => {
					switchWatcher(path);

					VisualStudioHelper.AddFileToSolution(environment, path);
					VisualStudioHelper.OpenFile(environment, path);
				});

			environmentSettingsChanged
				.Throttle(TimeSpan.FromMilliseconds(300))
				.Select(_ => SettingApplier.Extract(environment, applicationRegistryRoot))
				.SelectMany(async settings => {
					watcher.Pause();
					await SettingsPersister.SaveAsync(settingsPath, settings);
					watcher.Resume();
					return Unit.Default;
				})
				.Subscribe(_ => visualStudioStatusBar.UpdateStatus("Settings saved"));


			// Ignore closed if there is an opened after it.
			var openedOrClosedSolution = Observable
				.Merge(solutionOpened, solutionClosed)
				.Throttle(TimeSpan.FromSeconds(0.5))
				.Where(newSettingsPath => settingsPath != newSettingsPath);

			fileSettingsChanged = fileSettingsChanged
				.Throttle(TimeSpan.FromSeconds(0.5));

			fileSettingsDeleted = fileSettingsDeleted
				.Throttle(TimeSpan.FromSeconds(0.5))
				.Select(_ => GetCurrentSettingsPath());

			Observable
				.Merge(vsReady, openedOrClosedSolution, fileSettingsChanged, fileSettingsDeleted)
				.TakeUntil(VisualStudioShutdown())
				//.Trace("reload - TakeUntil")
				.SelectMany(async newSettingsPath => new { Path = newSettingsPath, Settings = await SettingsPersister.LoadAsync(newSettingsPath) })
				//.Trace("reload - SelectMany")
				.Subscribe(x => {
					SettingApplier.Apply(environment, x.Settings);
					switchWatcher(x.Path);
				});

			Debug.WriteLine("Events wired up!");
		}

		#region Events

		private IObservable<string> SettingsFileChanged(SettingsFileWatcher watcher)
		{
			return Observable.FromEventPattern<string>(
				ev => watcher.OnSettingsFileChanged += ev,
				ev => watcher.OnSettingsFileChanged -= ev)
				.Select(x => x.EventArgs);
		}

		private IObservable<string> SettingsFileDeleted(SettingsFileWatcher watcher)
		{
			return Observable.FromEventPattern<string>(
				ev => watcher.OnSettingsFileDeleted += ev,
				ev => watcher.OnSettingsFileDeleted -= ev)
				.Select(x => x.EventArgs);
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

		private IObservable<string> VisualStudioReady()
		{
			return Observable.Return(GetGlobalSettingsPath());
		}

		private IObservable<string> SolutionOpened()
		{
			var solution = environment.Events.SolutionEvents;
			return Observable
                .FromEvent<_dispSolutionEvents_OpenedEventHandler, Unit>(
				    c => () => c(Unit.Default),
				    x => solution.Opened += x,
				    x => solution.Opened -= x)
                .Select(_ => GetCurrentSettingsPath());
		}

        private IObservable<string> SolutionClosed()
		{
			var solution = environment.Events.SolutionEvents;
			return Observable
                .FromEvent<_dispSolutionEvents_AfterClosingEventHandler, Unit>(
				    c => () => c(Unit.Default), 
				    x => solution.AfterClosing += x,
				    x => solution.AfterClosing -= x)
                .Select(_ => GetGlobalSettingsPath());
		}

		private IObservable<Unit> VisualStudioShutdown()
		{
			var dte = environment.Events.DTEEvents;
			return Observable.FromEvent<_dispDTEEvents_OnBeginShutdownEventHandler, Unit>(
				c => () => c(Unit.Default),
				x => dte.OnBeginShutdown += x,
				x => dte.OnBeginShutdown -= x);
		}

		#endregion
	}
}
