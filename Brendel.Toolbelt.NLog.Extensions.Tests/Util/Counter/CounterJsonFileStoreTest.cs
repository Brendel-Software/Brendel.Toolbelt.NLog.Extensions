using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using JetBrains.Annotations;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Counter;

[TestSubject(typeof(CounterJsonFileStore))]
public class CounterJsonFileStoreTest {
	[Fact]
	public void LoadState_loads_state() {
		using var testfile = new DisposableFile();
		File.WriteAllText(testfile.FullPath, """
											 {
											   "StartTimestamp": "2021-09-01T12:00:00Z",
											   "Count":3
											 }
											 """);

		var sut = new CounterJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2021, 9, 1, 12, 0, 0, DateTimeKind.Utc), result.StartTimestamp);
		Assert.Equal(3, result.Count);
	}

	[Fact]
	public void LoadState_returns_null_on_nonexisting_file() {
		using var testfile = new DisposableFile();
		var sut = new CounterJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.Null(result);
	}

	[Fact]
	public void LoadState_returns_null_on_empty_file() {
		using var testfile = new DisposableFile();
		File.WriteAllText(testfile.FullPath, "");

		var sut = new CounterJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.Null(result);
	}

	[Fact]
	public void SaveState_saves_state() {
		using var testfile = new DisposableFile();
		const string expected_json = "{\"StartTimestamp\":\"2024-10-17T12:32:12Z\",\"Count\":44}";
		var state = new TimestampedCounter {
			StartTimestamp = new DateTime(2024, 10, 17, 12, 32, 12, DateTimeKind.Utc),
			Count = 44
		};
		var sut = new CounterJsonFileStore(testfile.FullPath);
		sut.SaveState(state);

		var json = File.ReadAllText(testfile.FullPath);
		Assert.Equal(expected_json, json);
	}

	[Fact]
	public void SaveState_creates_subdirectories_on_demand() {
		using var subdir = new DisposableDirectory();
		using var testfile = subdir.CreateFile();

		var state = new TimestampedCounter {
			StartTimestamp = new DateTime(2024, 10, 17, 12, 32, 12, DateTimeKind.Utc),
			Count = 44
		};

		var sut = new CounterJsonFileStore(testfile.FullPath);
		sut.SaveState(state);

		Assert.True(Directory.Exists(subdir.FullPath));
		Assert.True(File.Exists(testfile.FullPath));
	}

	[Fact]
	public void DeleteState_deletes_state() {
		using var testfile = new DisposableFile();
		var store = new CounterJsonFileStore(testfile.FullPath);
		store.DeleteState();

		Assert.False(File.Exists(testfile.FullPath));
	}
}