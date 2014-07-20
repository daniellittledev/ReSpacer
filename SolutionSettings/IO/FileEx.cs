using System;
using System.IO;
using System.Threading.Tasks;

namespace Enexure.SolutionSettings.IO
{
	public static class FileEx
	{
		public static Task WriteAsync(string path, Func<StreamWriter, Task> doWrite, FileMode fileMode = FileMode.OpenOrCreate, FileShare fileShare = FileShare.Read, int bufferSize = 4096)
		{
			using (var file = new FileStream(path, fileMode, FileAccess.Write, fileShare, bufferSize, FileOptions.Asynchronous))
			using (var writer = new StreamWriter(file)) {

				return doWrite(writer);
			}
		}

		public static Task ReadAsync(string path, Func<StreamReader, Task> doRead, FileShare fileShare = FileShare.Read, int bufferSize = 4096)
		{
			using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, fileShare, bufferSize, FileOptions.Asynchronous))
			using (var reader = new StreamReader(file)) {

				return doRead(reader);
			}
		}
	}
}
