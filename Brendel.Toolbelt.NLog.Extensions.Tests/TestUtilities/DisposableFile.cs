namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

public class DisposableFile : IDisposable {
	public string FullPath { get; }

	public DisposableFile(string? name = null, string? directory = null) {
		string path;

		if (name == null && directory == null) {
			path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		} else if (name != null && directory == null) {
			path = name;
		} else if (name == null && directory != null) {
			path = Path.Combine(directory, Path.GetRandomFileName());
		} else {
			path = Path.Combine(directory!, name!);
		}

		path = Path.Combine(path);

		if (File.Exists(path)) {
			throw new ArgumentException("Datei existiert bereits");
		}

		FullPath = path;
	}

	public void Dispose() {
		if (File.Exists(FullPath)) {
			File.Delete(FullPath);
		}
	}
}