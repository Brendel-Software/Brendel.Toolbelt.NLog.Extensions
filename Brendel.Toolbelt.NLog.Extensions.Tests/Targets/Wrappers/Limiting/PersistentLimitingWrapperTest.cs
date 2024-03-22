using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(PersistentLimitingWrapper))]
public class PersistentLimitingWrapperTest {
	private static (PersistentLimitingWrapper, CountingSpyTarget, Logger) CreateTestComponents(int messageLimit, TimeSpan interval, string file) {
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
		var logFactory = new LogFactory().Setup().LoadConfigurationFromXml(xml).LogFactory;
		var wrapper = (PersistentLimitingWrapper) logFactory.Configuration.FindTargetByName("wrapper");
		var target = (CountingSpyTarget) wrapper.WrappedTarget;
		var logger = logFactory.GetCurrentClassLogger();

		return (wrapper, target, logger);
	}

	[Fact]
	public void Initialize_restores_state_from_file() {
		// Arrange
		using var testFile = new DisposableFile();
		File.WriteAllText(testFile.FullPath, JsonConvert.SerializeObject(new LimitingWrapperState {
			IntervalStartUtc = DateTime.UtcNow - TimeSpan.FromMinutes(10),
			WriteCount = 7
		}));
		var (wrapper, countingTarget, logger) = CreateTestComponents(10, TimeSpan.FromHours(1), testFile.FullPath);

		// Act
		logger.WriteFakeDebugMessages(20);

		// Assert
		Assert.Equal(3, countingTarget.WrittenMessagesCounter);
	}

	[Fact]
	public void Close_saves_state_to_file() {
		// Arrange
		using var testFile = new DisposableFile();
		var (wrapper, countingTarget, logger) = CreateTestComponents(10, TimeSpan.FromHours(1), testFile.FullPath);

		// Act
		logger.WriteFakeDebugMessages(20);
		logger.Factory.Shutdown();

		// Assert
		var state = JsonConvert.DeserializeObject<LimitingWrapperState>(File.ReadAllText(testFile.FullPath));
		Assert.Equal(10, state?.WriteCount);
	}
}