namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

public class DisposableDirectory : IDisposable {
	public string FullPath { get; }

	public DisposableDirectory(string? dir = null) {
		string path;
		if (!string.IsNullOrWhiteSpace(dir)) {
			path = Path.GetFullPath(dir);
		} else {
			path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
		}

		if (Directory.Exists(path)) {
			throw new ArgumentException("Verzeichnis existiert bereits");
		}

		FullPath = path;
	}

	public DisposableFile CreateFile(string? name = null) {
		return new DisposableFile(name, FullPath);
	}

	public void Dispose() {
		if (Directory.Exists(FullPath)) {
			Directory.Delete(FullPath);
		}
	}
}