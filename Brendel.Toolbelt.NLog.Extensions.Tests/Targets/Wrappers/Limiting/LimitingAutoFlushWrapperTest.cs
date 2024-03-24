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
		bool flushOnConditionOnly = true,
		bool debounceDiscardedFlushes = false
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
									flushOnConditionOnly="{{flushOnConditionOnly}}"
									debounceDiscardedFlushes="{{debounceDiscardedFlushes}}">
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
	public void Flush_or_Shutdown_triggers_flush_when_FlushOnConditionOnly_is_false() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(3, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false, false);
		logger.Factory.Flush();
		Assert.Equal(1, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Shutdown();
		Assert.Equal(2, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void Flush_or_Shutdown_does_not_trigger_flush_when_FlushOnConditionOnly_is_false_and_limit_reached() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(3, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false, false);
		logger.Factory.Flush();
		logger.Factory.Flush();
		logger.Factory.Flush();
		Assert.Equal(3, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Flush();
		Assert.Equal(3, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Shutdown();
		Assert.Equal(3, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void Flush_or_Shutdown_does_not_trigger_flush_when_FlushOnConditionOnly_is_true() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(3, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", true, false);
		logger.Factory.Flush();
		Assert.Equal(0, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Shutdown();
		Assert.Equal(0, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public void Flush_or_Shutdown_force_debounce_completion() {
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(1, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", true, true);

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(1, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Flush();
		Assert.Equal(2, wrappedTarget.FlushOperationsCounter);

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(2, wrappedTarget.FlushOperationsCounter);
		logger.Factory.Shutdown();
		Assert.Equal(3, wrappedTarget.FlushOperationsCounter);
	}

	[Fact]
	public async Task Write_triggers_delayed_flush_when_DebounceDiscardedFlushes_is_set() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.Now);
		var (logger, wrapper, wrappedTarget) = CreateTestComponents(5, TimeSpan.FromMinutes(5), "level >= LogLevel.Warn", false, true);
		wrapper.TimeProvider = fakeTimeProvider;

		logger.WriteFakeWarnMessages(10);
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		await fakeTimeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		await fakeTimeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(2));
		Assert.Equal(5, wrappedTarget.FlushOperationsCounter);

		await fakeTimeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(2));
		Assert.Equal(6, wrappedTarget.FlushOperationsCounter);
	}
}

public static class FakeTimeProviderExtensions {
	/// <summary>
	/// Waits one tick to ensure that other Tasks have started
	/// </summary>
	public static async Task YieldOneTickAndAdvance(this FakeTimeProvider timeProvider, TimeSpan delta) {
		await Task.Delay(1);
		timeProvider.Advance(TimeSpan.FromTicks(1));
		timeProvider.Advance(delta);
	}
}