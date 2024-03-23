using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities.Targets;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(StatefulLimitingTargetWrapper))]
public class StatefulLimitingTargetWrapperTest {
	private (Logger, StatefulLimitingTargetWrapper, CountingSpyTarget) CreateTestComponents(int messageLimit, TimeSpan interval) {
		var xml = $$"""
					<nlog throwConfigExceptions="true">
						<extensions>
							<add assembly="Brendel.Toolbelt.NLog.Extensions.Tests" />
							<add assembly="Brendel.Toolbelt.NLog.Extensions" />
						</extensions>
						
						<targets>
							<wrapper-target name="wrapper" type="StatefulLimitingWrapper"
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
		return TestComponentsFactory.BuildWrapperTestComponentsFromXml<StatefulLimitingTargetWrapper, CountingSpyTarget>(xml);
	}

	[Fact]
	public void Write_discards_messages_when_message_limit_reached() {
		var (logger, _, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));

		logger.WriteFakeDebugMessages(20);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	public void Write_stops_discarding_messages_after_interval_passed() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var fakeTimeProvider = new FakeTimeProvider();
		sut.TimeProvider = fakeTimeProvider;

		logger.WriteFakeDebugMessages(7);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(6));
		logger.WriteFakeDebugMessages(1);
		Assert.Equal(6, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	public void Write_retains_correct_limits_between_intervals() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);
		sut.TimeProvider = fakeTimeProvider;

		logger.WriteFakeDebugMessages(7);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(6));
		logger.WriteFakeDebugMessages(4);
		Assert.Equal(9, wrappedTarget.WrittenMessagesCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(6));
		logger.WriteFakeDebugMessages(3);
		Assert.Equal(12, wrappedTarget.WrittenMessagesCounter);

		logger.WriteFakeDebugMessages(8);
		Assert.Equal(14, wrappedTarget.WrittenMessagesCounter);
	}

	private LoggingConfiguration CreateMiniumLoggingConfigurationWithGivenWrapper(StatefulLimitingTargetWrapper wrapper) {
		var config = new LoggingConfiguration {
			LogFactory = {
				ThrowConfigExceptions = true
			}
		};
		wrapper.WrappedTarget = new DebugTarget {
			Layout = "${message}"
		};
		config.AddTarget("limiting", wrapper);
		config.AddRuleForAllLevels(wrapper);
		return config;
	}

	[Fact]
	public void Initialization_fails_on_missing_message_limit() {
		var sut = new StatefulLimitingTargetWrapper {
			MessageLimit = new(0)
		};

		var config = CreateMiniumLoggingConfigurationWithGivenWrapper(sut);

		var ex = Assert.Throws<NLogConfigurationException>(() => new LogFactory().Setup().LoadConfiguration(config));
		Assert.Contains("MessageLimit property must be > 0", ex.Message);
	}

	[Fact]
	public void Initialization_fails_on_missing_interval() {
		var sut = new StatefulLimitingTargetWrapper {
			MessageLimit = new(5),
			Interval = new(TimeSpan.Zero)
		};

		var config = CreateMiniumLoggingConfigurationWithGivenWrapper(sut);

		var ex = Assert.Throws<NLogConfigurationException>(() => new LogFactory().Setup().LoadConfiguration(config));
		Assert.Contains("Interval property must be > 0", ex.Message);
	}
}