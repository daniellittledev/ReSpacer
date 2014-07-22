using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Enexure.SolutionSettings.Services
{
	static class VisualStudioHelper
	{
		public static void AddFileToSolution(DTE environment, string filePath)
		{
			// Select the solution
			var window = environment.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
			var hierarchy = window.Object as UIHierarchy;
			Debug.Assert(hierarchy != null, "hierarchy != null");
			var rootItem = hierarchy.UIHierarchyItems.Item(1);
			rootItem.Select(vsUISelectionType.vsUISelectionTypeSelect);

			environment.ItemOperations.AddExistingItem(filePath);
		}

		public static void OpenFile(DTE environment, string filePath)
		{
			environment.ItemOperations.OpenFile(filePath);
		}

        public static void WriteTo(IVsOutputWindowPane windowPane, string text)
        {
            if (windowPane == null) throw new ArgumentNullException("windowPane");

            if (Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.OutputString(text))) {
                Debug.WriteLine("Failed to write on the Output window");
            }
        }
	}
}
