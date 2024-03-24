using Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;
using Brendel.Toolbelt.NLog.Extensions.Util.Concurrency;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Concurrency;

[TestSubject(typeof(DebounceHelper))]
public class DebounceHelperTest {

	[Fact]
	public async Task DebounceAt_calls_Action_on_debounce() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(1));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));

		Assert.True(await tcs.Task);
	}

	[Fact]
	public async Task DebounceAt_does_not_call_Action_on_repeated_call() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var callCount = 0;
		helper.Action = _ => {
			callCount++;
		};

		var debounceAt = timeProvider.GetUtcNow().AddMinutes(3);
		helper.DebounceAt(debounceAt);
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(debounceAt);
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(debounceAt);
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));

		Assert.Equal(1, callCount);
	}

	[Fact]
	public async Task DebounceAt_does_recognize_sliding_window() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var callCount = 0;
		helper.Action = _ => {
			callCount++;
		};

		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(2));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(2));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(2));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(2));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(10));

		Assert.Equal(1, callCount);
	}

	[Fact]
	public async Task DebounceAt_calls_action_when_time_has_advanced() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		var now = timeProvider.GetUtcNow().AddMinutes(1);
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.DebounceAt(now);
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));

		Assert.True(await tcs.Task);
	}

	[Fact]
	public async Task Cancel_cancels_current_debounce_schedule() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(3));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.Cancel();
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(3));

		Assert.False(tcs.Task.IsCompleted);
	}

	[Fact]
	public async Task Dispose_cancels_current_debounce_schedule() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(timeProvider.GetUtcNow().AddMinutes(2));
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(1));
		helper.Dispose();
		await timeProvider.YieldOneTickAndAdvance(TimeSpan.FromMinutes(3));

		Assert.False(tcs.Task.IsCompleted);
	}

	[Fact]
	public void EnqueueAt_throws_when_disposed() {
		var sut = new DebounceHelper(new FakeTimeProvider());
		sut.Dispose();
		Assert.Throws<ObjectDisposedException>(() => sut.DebounceAt(DateTimeOffset.Now));
	}
}