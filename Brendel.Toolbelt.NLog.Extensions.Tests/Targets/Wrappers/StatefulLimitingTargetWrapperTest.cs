using System.Diagnostics.CodeAnalysis;
using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using JetBrains.Annotations;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers;

[TestSubject(typeof(StatefulLimitingTargetWrapper))]
public class StatefulLimitingTargetWrapperTest {
	private class CountingTargetSpy : Target {
		public int WrittenMessagesCounter { get; private set; }

		protected override void Write(LogEventInfo _) {
			WrittenMessagesCounter += 1;
		}
	}

	private static string CreateConfigurationXmlText(int messageLimit, TimeSpan interval) {
		return $$"""
				 <nlog throwConfigExceptions="true">
				   
				   <extensions>
				     <add assembly="Brendel.Toolbelt.NLog.Extensions" />
				   </extensions>
				   
				   <targets>
				     <wrapper-target name="limiting" type="StatefulLimitingWrapper" messageLimit="{{messageLimit}}" interval="{{interval:c}}">
				       <target name='debug' type='Debug' layout='${message}' />
				 	 </wrapper-target>
				   </targets>
				   
				   <rules>
				     <logger name='*' level='Debug' writeTo='limiting' />
				   </rules>

				 </nlog>
				 """;
	}

	private (Logger, StatefulLimitingTargetWrapper, CountingTargetSpy) CreateTestComponents(int messageLimit, TimeSpan interval) {
		// create a logger with a limiting wrapper from XML
		var xml = CreateConfigurationXmlText(messageLimit, interval);
		var logFactory = new LogFactory().Setup().LoadConfigurationFromXml(xml).LogFactory;
		var logger = logFactory.GetLogger("Test");
		var wrapper = (StatefulLimitingTargetWrapper) logFactory.Configuration.FindTargetByName("limiting");

		// exchange the wrapped target with a counting target spy
		var target = new CountingTargetSpy();
		wrapper.WrappedTarget = target;
		logFactory.ReconfigExistingLoggers();

		// return the logger, the wrapper and the counting target spy
		return (logger, wrapper, target);
	}

	[Fact]
	public void Write_discards_messages_when_message_limit_reached() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));

		logger.Debug("1");
		logger.Debug("2");
		logger.Debug("3");
		logger.Debug("4");
		logger.Debug("5");
		logger.Debug("6");
		logger.Debug("7");

		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	public void Write_stops_discarding_messages_after_interval_passed() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var offsetMinutes = 0;
		sut.TimeProvider = () => DateTime.UtcNow.AddMinutes(offsetMinutes);

		logger.Debug("1");
		logger.Debug("2");
		logger.Debug("3");
		logger.Debug("4");
		logger.Debug("5");
		logger.Debug("6");
		logger.Debug("7");
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);
		offsetMinutes += 5; // Move time forward by 5 minutes
		logger.Debug("8");
		Assert.Equal(6, wrappedTarget.WrittenMessagesCounter);
	}

	[Fact]
	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	public void Write_retains_correct_limits_between_intervals() {
		var (logger, sut, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5));
		var offsetMinutes = 0;
		sut.TimeProvider = () => DateTime.UtcNow.AddMinutes(offsetMinutes);

		logger.Debug("1");
		logger.Debug("2");
		logger.Debug("3");
		logger.Debug("4");
		logger.Debug("5");
		logger.Debug("6");
		logger.Debug("7");
		Assert.Equal(5, wrappedTarget.WrittenMessagesCounter);

		offsetMinutes += 5; // Move time forward by 5 minutes
		logger.Debug("8");
		logger.Debug("9");
		logger.Debug("10");
		logger.Debug("11");
		Assert.Equal(9, wrappedTarget.WrittenMessagesCounter);

		offsetMinutes += 5; // Move time forward by another 5 minutes
		logger.Debug("12");
		logger.Debug("13");
		logger.Debug("14");
		Assert.Equal(12, wrappedTarget.WrittenMessagesCounter);

		logger.Debug("15");
		logger.Debug("16");
		logger.Debug("17");
		logger.Debug("18");
		logger.Debug("19");
		logger.Debug("20");
		Assert.Equal(14, wrappedTarget.WrittenMessagesCounter);
	}

	private LoggingConfiguration CreateBareMiniumLoggingConfigurationWithWrapper(StatefulLimitingTargetWrapper wrapper) {
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

		var config = CreateBareMiniumLoggingConfigurationWithWrapper(sut);

		var ex = Assert.Throws<NLogConfigurationException>(() => new LogFactory().Setup().LoadConfiguration(config));
		Assert.Contains("MessageLimit property must be > 0", ex.Message);
	}

	[Fact]
	public void Initialization_fails_on_missing_interval() {
		var sut = new StatefulLimitingTargetWrapper {
			MessageLimit = new(5),
			Interval = new(TimeSpan.Zero)
		};

		var config = CreateBareMiniumLoggingConfigurationWithWrapper(sut);

		var ex = Assert.Throws<NLogConfigurationException>(() => new LogFactory().Setup().LoadConfiguration(config));
		Assert.Contains("Interval property must be > 0", ex.Message);
	}
}