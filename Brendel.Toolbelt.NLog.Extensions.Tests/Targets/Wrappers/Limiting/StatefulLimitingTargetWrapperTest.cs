using System.Diagnostics.CodeAnalysis;
using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using JetBrains.Annotations;
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
		var logFactory = new LogFactory().Setup().LoadConfigurationFromXml(xml).LogFactory;
		var wrapper = (StatefulLimitingTargetWrapper) logFactory.Configuration.FindTargetByName("wrapper");
		var target = (CountingSpyTarget) wrapper.WrappedTarget;
		var logger = logFactory.GetCurrentClassLogger();

		return (logger, wrapper, target);
	}

	[Fact]
	public void Write_discards_messages_when_message_limit_reached() {
		var (logger, _, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));

		logger.WriteFakeDebugMessages(20);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	public void Write_stops_discarding_messages_after_interval_passed() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var offsetMinutes = 0;
		sut.TimeProvider = () => DateTime.UtcNow.AddMinutes(offsetMinutes);

		logger.WriteFakeDebugMessages(7);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);

		offsetMinutes += 5; // Move time forward by 5 minutes
		logger.WriteFakeDebugMessages(1);
		Assert.Equal(6, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	public void Write_retains_correct_limits_between_intervals() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var offsetMinutes = 0;
		sut.TimeProvider = () => DateTime.UtcNow.AddMinutes(offsetMinutes);

		logger.WriteFakeDebugMessages(7);
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);

		offsetMinutes += 5; // Move time forward by 5 minutes
		logger.WriteFakeDebugMessages(4);
		Assert.Equal(9, wrappedTarget.WrittenMessagesCounter);

		offsetMinutes += 5; // Move time forward by another 5 minutes
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