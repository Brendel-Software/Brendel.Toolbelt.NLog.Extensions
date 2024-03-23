using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using JetBrains.Annotations;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Counter;

[TestSubject(typeof(TimestampedCounterJsonFileStore.Builder))]
public class IntervalCounterJsonFileStoreTestBuilderTest {
	[Fact]
	public void UseTargetName_configures_File_property_by_target_name() {
		const string expected_file_name_pattern = "^NLog-.*Counter.*Store.*-.*-WllyWnkaCnsol.state.json$";
		var target = new ConsoleTarget {Name = @"🐱 Wìlly Wönka Cönsolü 🐭"};
		var sut = new TimestampedCounterJsonFileStore.Builder();
		sut.UseTargetName(target);

		Assert.NotNull(sut.File);

		var filename = Path.GetFileName(sut.File);
		Assert.Matches(expected_file_name_pattern, filename);

		var directory = Path.GetDirectoryName(sut.File)!.NormalizeAsDirectoryPath();
		Assert.Equal(Path.GetTempPath().NormalizeAsDirectoryPath(), directory);
	}

	[Fact]
	public void Build_builds_store_with_specified_file() {
		using var testFile = new DisposableFile();
		var sut = new TimestampedCounterJsonFileStore.Builder();
		sut.File = testFile.FullPath;

		var store = sut.Build();
		store.SaveState(new TimestampedCounter {Count = 1});

		Assert.NotEmpty(File.ReadAllText(testFile.FullPath));
	}
}