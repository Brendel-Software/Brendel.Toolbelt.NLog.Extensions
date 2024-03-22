namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

public static class Normalization {
	/// <summary>
	/// Removes the trailing directory separator from the path.
	/// </summary>
	public static string NormalizeAsDirectoryPath(this string path) {
		if (path.EndsWith(Path.DirectorySeparatorChar)) {
			return path[..^1];
		}

		return path;
	}
}