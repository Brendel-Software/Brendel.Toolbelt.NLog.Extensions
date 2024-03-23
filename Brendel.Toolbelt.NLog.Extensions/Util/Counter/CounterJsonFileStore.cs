using System.Reflection;
using System.Text.Json;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Util.Counter;

/// <summary>
/// Represents a class responsible for saving and loading a <see cref="TimestampedCounter"/> to and from a JSON file.
/// </summary>
public class CounterJsonFileStore : ICounterStore {
	private readonly string _file;

	/// <summary>
	/// Represents a class responsible for saving and loading a <see cref="TimestampedCounter"/> to and from a JSON file.
	/// </summary>
	/// <param name="file">The file path where the <see cref="TimestampedCounter"/> will be saved to or loaded from. This parameter cannot be null or empty.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="file"/> is null or empty.</exception>
	public CounterJsonFileStore(string file) {
		if (string.IsNullOrWhiteSpace(file)) {
			throw new ArgumentException("File must not be null or empty", nameof(file));
		}

		_file = file;
	}

	public TimestampedCounter? LoadState() {
		if (!File.Exists(_file)) {
			return null;
		}

		if (File.ReadAllText(_file) is not { } json || string.IsNullOrWhiteSpace(json)) {
			return null;
		}

		// deserialize json
		var state = JsonSerializer.Deserialize<TimestampedCounter>(json);
		return state;
	}

	public void SaveState(TimestampedCounter state) {
		// ensure Directory exists
		if (Path.GetDirectoryName(_file) is { } dir && !Directory.Exists(dir)) {
			Directory.CreateDirectory(dir);
		}

		// serialize as json
		var json = JsonSerializer.Serialize(state);
		File.WriteAllText(_file, json);
	}

	public void DeleteState() {
		File.Delete(_file);
	}

	/// <summary>
	/// Represents a builder for creating a <see cref="CounterJsonFileStore"/> instance.
	/// </summary>
	public class Builder {
		/// <summary>
		/// Gets or sets the file path used to store the state.
		/// </summary>
		/// <value>
		/// Must be specified before building.
		/// </value>
		public string? File { get; set; }

		/// <summary>
		/// Configures the builder to use a specific <see cref="Target.Name"/> for generating a state file path.
		/// </summary>
		/// <param name="target">The target used to generate the file name. Must not be <c>null</c>.</param>
		/// <remarks>
		/// This method generates a file name based on the target's name, the executing assembly's name, and a predefined prefix and extension.
		/// It then sets the <see cref="File"/> property to a full path combining the temporary directory with the generated file name.
		/// </remarks>
		public void UseTargetName(Target target) {
			const string prefix = $"NLog-{nameof(CounterJsonFileStore)}";
			var assemblyName = RemoveUnsafeCharacters(Assembly.GetExecutingAssembly().GetName().Name);
			var name = RemoveUnsafeCharacters(target.Name);
			const string ext = ".state.json";
			var stateFileName = $"{prefix}-{assemblyName}-{name}{ext}";
			var stateFilePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), stateFileName));
			File = stateFilePath;
		}

		/// <summary>
		/// Builds and returns a <see cref="CounterJsonFileStore"/> instance using the configured file path.
		/// </summary>
		/// <returns>A <see cref="CounterJsonFileStore"/> instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the <see cref="File"/> property is not set before calling this method.</exception>
		public CounterJsonFileStore Build() {
			if (File is not { } file || string.IsNullOrWhiteSpace(file)) {
				throw new InvalidOperationException("File must be set");
			}

			return new CounterJsonFileStore(file);
		}

		private static string RemoveUnsafeCharacters(string? input) =>
			CompiledExpressions.UnsafeFileNameCharacters().Replace(input ?? string.Empty, string.Empty);
	}
}