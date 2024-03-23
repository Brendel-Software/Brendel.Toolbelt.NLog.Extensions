using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities.Targets;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using NLog;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(LimitingAutoFlushWrapper))]
public class LimitingAutoFlushWrapperTest {
	private (Logger, LimitingAutoFlushWrapper, CountingSpyTarget) CreateTestComponents(
		int limit,
		TimeSpan interval,
		string condition,
		bool resetAfterNonConditionalFlush = true,
		bool debounceLostFlushes = false
	) {
		var xml = $$"""
					<?xml version="1.0" encoding="utf-8"?>

					<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xsi:schemaLocation="NLog NLog.xsd"
					      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
					      throwConfigExceptions="true">
						
						<extensions>
							<add assembly="Brendel.Toolbelt.NLog.Extensions.Tests" />
							<add assembly="Brendel.Toolbelt.NLog.Extensions" />
						</extensions>
						
						<targets>
							<target xsi:type="LimitingAutoFlushWrapper"
									name="wrapper"
									condition="{{condition}}"
									flushLimit="{{limit}}"
									interval="{{interval:c}}"
									resetAfterNonConditionalFlush="{{resetAfterNonConditionalFlush}}"
									debounceLostFlushes="{{debounceLostFlushes}}">
								<target xsi:type="CountingSpy"
								        name="counter" />
							</target>
						</targets>
						
						<rules>
							<logger name="*" writeTo="wrapper" />
						</rules>
					</nlog>
					""";
		return TestComponentsFactory.BuildWrapperTestComponentsFromXml<LimitingAutoFlushWrapper, CountingSpyTarget>(xml);
	}

	[Fact]
	public void Write_triggers_flush_when_below_limit() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn");
		logger.WriteFakeWarnMessages(4);
		Assert.Equal(4, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void Write_does_not_trigger_flush_on_reached_limit() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn");
		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void Write_does_not_trigger_flush_on_reached_limit_until_interval_passed() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.Now);
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn");
		wrapper.TimeProvider = timeProvider;

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		timeProvider.Advance(TimeSpan.FromMinutes(2));
		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		timeProvider.Advance(TimeSpan.FromMinutes(2));
		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		timeProvider.Advance(TimeSpan.FromMinutes(2));
		logger.WriteFakeWarnMessages(1);
		Assert.Equal(6, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void FlushAsync_resets_counter_when_ResetAfterNonConditionalFlushIsSet() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", true);
		logger.Factory.Shutdown();
		Assert.Equal(0, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void FlushAsync_does_not_trigger_flush_when_ResetAfterNonConditionalFlush_is_false() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false);
		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Shutdown();
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void FlushAsync_does_trigger_flush_after_interval_end_when_DebounceLostFlushes_is_true() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false, true);
		wrapper.TimeProvider = fakeTimeProvider;

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
		Assert.Equal(6, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void CloseTarget_triggers_flush_when_debounce_active() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false, true);
		wrapper.TimeProvider = fakeTimeProvider;

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		fakeTimeProvider.Advance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		logger.Factory.Shutdown();
		Assert.Equal(6, wrappedTarget.FlushOperationsCounter);
	}
}