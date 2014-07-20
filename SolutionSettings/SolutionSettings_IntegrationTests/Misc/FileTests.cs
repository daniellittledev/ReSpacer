using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SolutionSettings_IntegrationTests.Misc
{
	[TestClass]
	public class FileTests
	{
		[TestMethod]
		public async Task TestForFile()
		{
			var path = "sample.txt";

			if (File.Exists(path)) {
				File.Delete(path);
			}

			using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.Asynchronous)) {
				try {
					using (var reader = new StreamReader(file)) {

						await reader.ReadToEndAsync();
					}
					return;
				} catch (Exception exception) {

					Assert.IsTrue(exception is FileNotFoundException);
				}

				using (var writer = new StreamWriter(file)) {

					await writer.WriteAsync("Hello world");
				}
			}
		}
	}
}
