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
		private enum SettingsOption
		{
			Global,
			Solution
		}

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

		private SettingsOption activeSetting;
		private string currentSettingsPath;

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
#if DEBUG
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Seq("http://localhost:5341")
				.CreateLogger();
#endif

			WireUpEvents();

		}

		private void WireUpEvents()
		{
			var vsReady = VisualStudioReady();
			var solutionSettingsAdding = SolutionSettingsAdding();
			var solutionOpened = SolutionOpened();
			var solutionClosed = SolutionClosed();
			var environmentSettingsChanged = EnvironmentSettingsChanged();

			var gloablWatcher = new SettingsFileWatcher(settingsFileName);
			gloablWatcher.SwitchTo(GetGlobalSettingsPath());
			var globalSettingsChanged = gloablWatcher.OnSettingsFileChanged.Select(x => x.FullPath);

			var solutionWatcher = new SettingsFileWatcher(settingsFileName);
			var solutionSettingsCreated = solutionWatcher.OnSettingsFileCreated.Select(x => x.FullPath);
			var solutionSettingsChanged = solutionWatcher.OnSettingsFileChanged.Select(x => x.FullPath);
			var solutionSettingsDeleted = solutionWatcher.OnSettingsFileDeleted.Select(x => x.FullPath);

			var activeWatcher = (Func<SettingsFileWatcher>)(() => {
				return (activeSetting == SettingsOption.Global) ? gloablWatcher : solutionWatcher;
			});

			var switchActiveSettings = (Action<string>)(newSettingsPath => {

				if (currentSettingsPath != newSettingsPath) {

					var globalSettings = GetGlobalSettingsPath();

					activeSetting = (newSettingsPath == globalSettings) ? SettingsOption.Global : SettingsOption.Solution;
					currentSettingsPath = newSettingsPath;

					if (activeSetting == SettingsOption.Solution) {
						solutionWatcher.SwitchTo(newSettingsPath);
					}
				}

				visualStudioStatusBar.UpdateStatus(activeSetting == SettingsOption.Global ? "Loaded global ReSpacer settings" : "Loaded solution ReSpacer settings");
			});

			vsReady
				.Where(path => !File.Exists(path))
				.Trace("vsReady - Select")
				.SelectMany(async path => {
					var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
					await SettingsPersister.SaveAsync(path, settings);
					return path;
				})
				.Trace("vsReady - SelectMany")
				.Subscribe(switchActiveSettings);

			solutionSettingsAdding
				.Select(_ => GetSolutionSettingsPath())
				.Where(path => !File.Exists(path))
				.SelectMany(async path => {
					
					var settings = SettingApplier.Extract(environment, applicationRegistryRoot);
					await SettingsPersister.SaveAsync(path, settings);

					return path;
				})
				.Trace("solutionSettingsAdding - SelectMany")
				.Subscribe(path => {
					switchActiveSettings(path);

					VisualStudioHelper.AddFileToSolution(environment, path);
					VisualStudioHelper.OpenFile(environment, path);
				});

			environmentSettingsChanged
				.Throttle(TimeSpan.FromMilliseconds(300))
				.Select(_ => SettingApplier.Extract(environment, applicationRegistryRoot))
				.SelectMany(async settings => {
					activeWatcher().Pause();
					await SettingsPersister.SaveAsync(currentSettingsPath, settings);
					activeWatcher().Resume();
					return Unit.Default;
				})
				.Trace("environmentSettingsChanged - SelectMany")
				.Subscribe(_ => visualStudioStatusBar.UpdateStatus("Settings saved"));


			// Ignore closed if there is an opened after it.
			var openedOrClosedSolution = Observable
				.Merge(solutionOpened, solutionClosed)
				.Throttle(TimeSpan.FromSeconds(0.5))
				.Where(newSettingsPath => currentSettingsPath != newSettingsPath);

			globalSettingsChanged = globalSettingsChanged
				.Where(path => path == currentSettingsPath)
				.Throttle(TimeSpan.FromSeconds(0.5));

			//solutionSettingsCreated = solutionSettingsCreated
			//	.NotAfter(solutionSettingsAdding)
			//	.Where(path => path == currentSettingsPath)
			//	.Throttle(TimeSpan.FromSeconds(0.5));

			solutionSettingsChanged = solutionSettingsChanged
				.Where(path => path == currentSettingsPath)
				.Throttle(TimeSpan.FromSeconds(0.5));

			solutionSettingsDeleted = solutionSettingsDeleted
				.Where(path => path == currentSettingsPath)
				.Throttle(TimeSpan.FromSeconds(0.5))
				.Select(_ => GetCurrentSettingsPath());

			Observable
				.Merge(vsReady, openedOrClosedSolution, solutionSettingsChanged, solutionSettingsDeleted, globalSettingsChanged)
				.TakeUntil(VisualStudioShutdown())
				.Trace("reload - TakeUntil")
				.SelectMany(async newSettingsPath => new {
					Path = newSettingsPath, 
					Settings = await SettingsPersister.LoadAsync(newSettingsPath)
				})
				.Trace("reload - SelectMany")
				.Subscribe(x => {
					SettingApplier.Apply(environment, x.Settings);
					switchActiveSettings(x.Path);
				});

			Debug.WriteLine("Events wired up!");
		}

		#region Events

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
