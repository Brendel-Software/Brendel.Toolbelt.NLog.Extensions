using Brendel.Toolbelt.NLog.Extensions.Util.Concurrency;
using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using NLog.Common;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Keeps track of the number of flush operations within a specified interval and limits them.
/// </summary>
[Target("LimitingAutoFlushWrapper")]
public class LimitingAutoFlushWrapper : AutoFlushTargetWrapper {
	private DebounceHelper? _debounceHelperHolder;

	private DebounceHelper DebounceHelper => _debounceHelperHolder ??= new(TimeProvider) {Action =	OnDebounceFinished };

	/// <summary>
	/// A Counter that keeps track of the number of flush operations within the <see cref="Interval" />.
	/// </summary>
	protected virtual TimestampedCounter Counter { get; set; } = new();

	/// <summary>
	/// Provides the date and time.
	/// </summary>
	public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

	/// <summary>
	/// Gets or sets the interval after which the internal counter will be reset.
	/// </summary>
	public TimeSpan Interval { get; set; } = TimeSpan.Zero;

	/// <summary>
	/// Gets or sets the limit of flush operations per <see cref="Interval" />.
	/// </summary>
	public int FlushLimit { get; set; } = 1;

	/// <summary>
	/// A flush operation will be called at the end of the interval when flush operations were discarded due to the <see cref="FlushLimit" /> being reached.
	/// </summary>
	public bool DebounceDiscardedFlushes { get; set; }

	protected override void Write(AsyncLogEventInfo logEvent) {
		if (CanFlush(logEvent)) {
			DebounceHelper.Cancel();
			Counter.IncrementIntervalAware(Interval, TimeProvider.GetUtcNow());
			base.Write(logEvent);
		} else {
			if (DebounceDiscardedFlushes) {
				DebounceHelper.DebounceAt(Counter.StartTimestamp + Interval);
			}

			WrappedTarget.WriteAsyncLogEvent(logEvent);
		}
	}

	protected override void FlushAsync(AsyncContinuation asyncContinuation) {
		if (DebounceHelper.Active) {
			DebounceHelper.Cancel();
			FlushWrappedTarget(asyncContinuation);
		} else if(FlushOnConditionOnly || LimitReached()) {
			asyncContinuation(null);
		} else {
			Counter.IncrementIntervalAware(Interval, TimeProvider.GetUtcNow());
			base.FlushAsync(asyncContinuation);
		}
	}

	protected override void CloseTarget() {
		_debounceHelperHolder?.Dispose();
		_debounceHelperHolder = null;
		base.CloseTarget();
	}

	private bool LimitReached() {
		return !Counter.CanIncrement(Interval, TimeProvider.GetUtcNow(), FlushLimit);
	}

	private bool CanFlush(AsyncLogEventInfo logEvent) {
		if (LimitReached()) {
			return false;
		}

		if (Condition.Evaluate(logEvent.LogEvent) is bool boolean) {
			return boolean;
		}

		return false;
	}

	private void OnDebounceFinished(DateTimeOffset debouncedAt) {
		if (Counter.StartTimestamp + Interval == debouncedAt) {
			FlushWrappedTarget(null);
		}
	}

	private void FlushWrappedTarget(AsyncContinuation? continuation) {
		WrappedTarget.Flush(continuation ?? (_ => { }));
	}
}