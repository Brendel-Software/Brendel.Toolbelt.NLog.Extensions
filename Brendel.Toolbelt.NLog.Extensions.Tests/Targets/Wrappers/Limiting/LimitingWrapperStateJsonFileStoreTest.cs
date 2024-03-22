using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using JetBrains.Annotations;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(LimitingWrapperStateJsonFileStore))]
public class LimitingWrapperStateJsonFileStoreTest {
	[Fact]
	public void LoadState_loads_state() {
		using var testfile = new DisposableFile();
		File.WriteAllText(testfile.FullPath, """
											 {
											   "IntervalStartUtc": "2021-09-01T12:00:00Z",
											   "WriteCount":3
											 }
											 """);

		var sut = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.NotNull(result);
		Assert.Equal(new DateTime(2021, 9, 1, 12, 0, 0, DateTimeKind.Utc), result.IntervalStartUtc);
		Assert.Equal(3, result.WriteCount);
	}

	[Fact]
	public void LoadState_returns_null_on_nonexisting_file() {
		using var testfile = new DisposableFile();
		var sut = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.Null(result);
	}

	[Fact]
	public void LoadState_returns_null_on_empty_file() {
		using var testfile = new DisposableFile();
		File.WriteAllText(testfile.FullPath, "");

		var sut = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		var result = sut.LoadState();
		Assert.Null(result);
	}

	[Fact]
	public void SaveState_saves_state() {
		using var testfile = new DisposableFile();
		const string expected_json = "{\"IntervalStartUtc\":\"2024-10-17T12:32:12Z\",\"WriteCount\":44}";
		var state = new LimitingWrapperState {
			IntervalStartUtc = new DateTime(2024, 10, 17, 12, 32, 12, DateTimeKind.Utc),
			WriteCount = 44
		};
		var sut = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		sut.SaveState(state);

		var json = File.ReadAllText(testfile.FullPath);
		Assert.Equal(expected_json, json);
	}

	[Fact]
	public void SaveState_creates_subdirectories_on_demand() {
		using var subdir = new DisposableDirectory();
		using var testfile = subdir.CreateFile();

		var state = new LimitingWrapperState {
			IntervalStartUtc = new DateTime(2024, 10, 17, 12, 32, 12, DateTimeKind.Utc),
			WriteCount = 44
		};

		var sut = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		sut.SaveState(state);

		Assert.True(Directory.Exists(subdir.FullPath));
		Assert.True(File.Exists(testfile.FullPath));
	}

	[Fact]
	public void DeleteState_deletes_state() {
		using var testfile = new DisposableFile();
		var store = new LimitingWrapperStateJsonFileStore(testfile.FullPath);
		store.DeleteState();

		Assert.False(File.Exists(testfile.FullPath));
	}
}