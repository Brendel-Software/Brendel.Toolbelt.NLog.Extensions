using Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;
using Brendel.Toolbelt.NLog.Extensions.Util.Concurrency;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Concurrency;

[TestSubject(typeof(DebounceHelper))]
public class DebounceHelperTest {
	[Fact]
	public async Task DebounceAt_calls_Action_on_debounce() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(fakeTimeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(1));
		await Task.Run(async () => {
			await Task.Delay(50);
			fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
			await Task.Delay(50);
		});

		Assert.True(await tcs.Task);
	}

	[Fact]
	public async Task DebounceAt_does_not_call_Action_on_repeated_call() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(fakeTimeProvider);
		var callCount = 0;
		helper.Action = _ => {
			callCount++;
		};

		var debounceAt = fakeTimeProvider.GetUtcNow().AddMinutes(3);

		helper.DebounceAt(debounceAt);
		await Task.Run(async () => {
			await Task.Delay(50);
			fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
			await Task.Delay(50);
		});

		helper.DebounceAt(debounceAt);
		await Task.Run(async () => {
			await Task.Delay(50);
			fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
			await Task.Delay(50);
		});

		helper.DebounceAt(debounceAt);
		await Task.Run(async () => {
			await Task.Delay(50);
			fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
			await Task.Delay(50);
		});

		Assert.Equal(1, callCount);
	}

	private Task AdvanceMinutesAsync(FakeTimeProvider timeProvider, int minutes) => Task.Run(async () => {
		await Task.Delay(50);
		timeProvider.Advance(TimeSpan.FromMinutes(minutes));
		await Task.Delay(50);
	});

	[Fact]
	public async Task DebounceAt_does_recognize_sliding_window() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(fakeTimeProvider);
		var callCount = 0;
		helper.Action = _ => {
			callCount++;
		};

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(2));
		await AdvanceMinutesAsync(fakeTimeProvider, 1);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(2));
		await AdvanceMinutesAsync(fakeTimeProvider, 1);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(2));
		await AdvanceMinutesAsync(fakeTimeProvider, 1);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(2));
		await AdvanceMinutesAsync(fakeTimeProvider, 2);

		Assert.Equal(1, callCount);
	}

	[Fact]
	public async Task DebounceAt_calls_action_when_time_has_advanced() {
		var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(timeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		var now = timeProvider.GetUtcNow().AddMinutes(1);
		await AdvanceMinutesAsync(timeProvider, 1);
		helper.DebounceAt(now);
		await AdvanceMinutesAsync(timeProvider, 1);

		Assert.True(await tcs.Task);
	}

	[Fact]
	public async Task Cancel_cancels_current_debounce_schedule() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(fakeTimeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(3));
		await AdvanceMinutesAsync(fakeTimeProvider, 1);
		helper.Cancel();
		await AdvanceMinutesAsync(fakeTimeProvider, 3);

		Assert.False(tcs.Task.IsCompleted);
	}

	[Fact]
	public async Task Dispose_cancels_current_debounce_schedule() {
		var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
		var helper = new DebounceHelper(fakeTimeProvider);
		var tcs = new TaskCompletionSource<bool>();
		helper.Action = _ => tcs.SetResult(true);

		helper.DebounceAt(fakeTimeProvider.GetUtcNow().AddMinutes(2));
		await AdvanceMinutesAsync(fakeTimeProvider, 1);
		helper.Dispose();
		await AdvanceMinutesAsync(fakeTimeProvider, 3);

		Assert.False(tcs.Task.IsCompleted);
	}

	[Fact]
	public void EnqueueAt_throws_when_disposed() {
		var sut = new DebounceHelper(new FakeTimeProvider());
		sut.Dispose();
		Assert.Throws<ObjectDisposedException>(() => sut.DebounceAt(DateTimeOffset.Now));
	}
}