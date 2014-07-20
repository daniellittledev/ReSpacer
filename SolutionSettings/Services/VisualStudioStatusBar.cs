using Microsoft.VisualStudio.Shell.Interop;

namespace Enexure.SolutionSettings.Services
{
	internal class VisualStudioStatusBar
	{
		private readonly IVsStatusbar statusbar;

		public VisualStudioStatusBar(IVsStatusbar statusbar)
		{
			this.statusbar = statusbar;
		}

		public void UpdateStatus(string status)
		{

			int frozen;

			statusbar.IsFrozen(out frozen);

			if (frozen == 0) {
				statusbar.SetText(status);
			}

		}
	}
}
