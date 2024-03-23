using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities.Targets;
using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(PersistentLimitingWrapper))]
public class PersistentLimitingWrapperTest {
	private static (Logger, PersistentLimitingWrapper, CountingSpyTarget) CreateTestComponents(int messageLimit, TimeSpan interval, string file) {
		var xml = $$"""
					<nlog throwConfigExceptions="true">
						<extensions>
							<add assembly="Brendel.Toolbelt.NLog.Extensions.Tests" />
							<add assembly="Brendel.Toolbelt.NLog.Extensions" />
						</extensions>
						
						<targets>
							<wrapper-target name="wrapper" type="PersistentLimitingWrapper"
											stateCacheFile="{{file}}"
										    messageLimit="{{messageLimit}}"
										    interval="{{interval:c}}">
								<target name="counter" type="CountingSpy" />
							</wrapper-target>
						</targets>
						
						<rules>
							<logger name='*' level='Debug' writeTo="wrapper" />
						</rules>
					</nlog>
					""";
		return TestComponentsFactory.BuildWrapperTestComponentsFromXml<PersistentLimitingWrapper, CountingSpyTarget>(xml);
	}

	[Fact]
	public void Initialize_restores_state_from_file() {
		// Arrange
		using var testFile = new DisposableFile();
		File.WriteAllText(testFile.FullPath, JsonConvert.SerializeObject(new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow - TimeSpan.FromMinutes(10),
			Count = 7
		}));
		var (logger, wrapper, countingTarget) = CreateTestComponents(10, TimeSpan.FromHours(1), testFile.FullPath);

		// Act
		logger.WriteFakeDebugMessages(20);

		// Assert
		Assert.Equal(3, countingTarget.WrittenMessagesCounter);
	}

	[Fact]
	public void Close_saves_state_to_file() {
		// Arrange
		using var testFile = new DisposableFile();
		var (logger, wrapper, countingTarget) = CreateTestComponents(10, TimeSpan.FromHours(1), testFile.FullPath);

		// Act
		logger.WriteFakeDebugMessages(20);
		logger.Factory.Shutdown();

		// Assert
		var state = JsonConvert.DeserializeObject<TimestampedCounter>(File.ReadAllText(testFile.FullPath));
		Assert.Equal(10, state?.Count);
	}
}