using System;
using System.Collections.Generic;
using System.Linq;
using Enexure.SolutionSettings.Settings;
using Enexure.SolutionSettings.Settings.Version2;
using EnvDTE;
using Microsoft.Win32;
using Serilog;

namespace Enexure.SolutionSettings.Services
{
	class SettingApplier
	{
		public static void Apply(DTE environment, VisualStudioSettings settings)
		{
			foreach (var propertyCollection in settings) {
				ApplyProperties(environment, propertyCollection.Name, propertyCollection.Settings);
			}
		}

		private static void ApplyProperties(DTE environment, string languageName, TextEditorSettings textEditorSettings)
		{
			Properties properties;
			var category = "TextEditor";

			try {
				properties = environment.Properties[category, languageName];
			} catch (InvalidCastException ex) {

				Log.Warning(ex, "ApplyProperties: Could not load properties in {category} for {page}", category, languageName);
				return;
			}

			var tabSettings = textEditorSettings.TabSettings;
			ApplyProperty(properties, "IndentStyle", tabSettings.IndentStyle);
			ApplyProperty(properties, "TabSize", tabSettings.TabSize);
			ApplyProperty(properties, "IndentSize", tabSettings.IndentSize);
			ApplyProperty(properties, "InsertTabs", tabSettings.InsertTabs);
		}

		private static void ApplyProperty(Properties properties, string name, object value)
		{
			if (value != null) {
				properties.Item(name).Value = value;
			}
		}

		public static VisualStudioSettings Extract(DTE environment, RegistryKey applicationRegistryRoot)
		{
			var settings = new VisualStudioSettings();
			settings.AddRange(ExtractSettings(environment, applicationRegistryRoot));

			return settings;
		}

		private static IEnumerable<SettingsPropertyCollection> ExtractSettings(DTE environment, RegistryKey applicationRegistryRoot)
		{
			return GetPropertyPages(applicationRegistryRoot)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(pageName => new SettingsPropertyCollection() {
					Name = pageName,
					Settings = ExtractProperties(environment, pageName)
				})
				.Where(x => x.Settings != null)
				.ToList();
		}

		private static TextEditorSettings ExtractProperties(DTE environment, string languageName)
		{
			Properties properties;
			var category = "TextEditor";

			try {
				properties = environment.Properties[category, languageName];
			} catch (InvalidCastException ex) {

				Log.Warning(ex, "ExtractProperties: Could not load properties in {category} for {page}", category, languageName);
				return null;
			}

			if (!HasProperty(properties, "IndentStyle")) {
				return null;
			}

			return new TextEditorSettings() {
				TabSettings = new TabSettings() {
					IndentStyle = GetValue<IndentStyle>(properties.Item("IndentStyle")),
					TabSize = GetValue<int>(properties.Item("TabSize")),
					IndentSize = GetValue<int>(properties.Item("IndentSize")),
					InsertTabs = GetValue<bool>(properties.Item("InsertTabs")),
				}
			};
		}

		private static IEnumerable<string> GetPropertyPages(RegistryKey rootKey)
		{
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

		private static bool HasProperty(Properties properties, string name)
		{
			try {
				properties.Item(name);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		private static T? GetValue<T>(Property item)
			where T : struct
		{
			try {
				return (T)Convert.ChangeType(item.Value, typeof(T));
			} catch (Exception) {
				return null;
			}
		}
	}
}
