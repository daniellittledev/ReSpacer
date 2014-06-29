#Solution Settings

This is a Visual Studio 2013 plugin that will allow per solution tab settings configuration.


## Global settings

When the plugin is run for the first time a global settings file will be created using the current visual studio settings.

If a solution does not contain a settings file these settings are used.

These defaults can be updated by navigating to `Tools` > `Open Global Text Settings` or by editing the `text.settings.json` file at the Visual Studio `UserDataPath`.

## Adding settings to a solution

Settings are stored in a `text.settings.json` file at the solution level, this file must be a sibling of the .snl file.

This file is watched for changes and updates will be automatically applied. Errors detected will pause setting updates.

To add a settings file to an existing project. Click `Add solution text settings`. You will see a dialog that lets you select the settings you want to override for this solution.

Examples include:

- Basic
- CSharp
- Plain Text

For each Editor option you select you can then override.

- IndentStyle (None, Default, Smart)
- TabSize (int)
- IndentSize (int)
- InsertTabs (bool)

## Get it now

[Download it now from the visual studio gallary](http://visualstudiogallery.msdn.microsoft.com/7a3c6a75-03e7-4a4d-9edf-ce6fd177ee40)