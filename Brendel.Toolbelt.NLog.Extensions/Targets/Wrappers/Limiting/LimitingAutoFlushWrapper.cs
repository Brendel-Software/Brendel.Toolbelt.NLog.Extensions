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
	/// A flush operation will be called when flush operations where lost due to the <see cref="FlushLimit" /> being reached,
	/// after the <see cref="Interval" /> has passed.
	/// </summary>
	public bool DebounceLostFlushes { get; set; }

	/// <summary>
	/// Controls whether the internal counter is reset implicitly after an explicit-flush, config-reload-flush and shutdown-flush.
	/// </summary>
	public bool ResetAfterNonConditionalFlush { get; set; } = true;

	protected virtual bool CanFlush(AsyncLogEventInfo? logEvent) {
		if (!Counter.CanIncrement(Interval, TimeProvider.GetUtcNow(), FlushLimit)) {
			return false;
		}

		if (!logEvent.HasValue) {
			return true;
		}

		if (Condition.Evaluate(logEvent.Value.LogEvent) is bool boolean) {
			return boolean;
		}

		return true;
	}

	protected override void Write(AsyncLogEventInfo logEvent) {
		if (CanFlush(logEvent)) {
			_debounceCts?.Cancel();
			Counter.IncrementIntervalAware(Interval, TimeProvider.GetUtcNow());
			base.Write(logEvent);
		} else {
			WrappedTarget.WriteAsyncLogEvent(logEvent);

			if (DebounceLostFlushes) {
				StartDebounceWindow();
			}
		}
	}

	protected override void FlushAsync(AsyncContinuation asyncContinuation) {
		if (ResetAfterNonConditionalFlush) {
			Counter.Reset();
		}

		if (CanFlush(null)) {
			_debounceCts?.Cancel();
			Counter.IncrementIntervalAware(Interval, TimeProvider.GetUtcNow());
			base.FlushAsync(asyncContinuation);
		} else {
			asyncContinuation(null);
			if (DebounceLostFlushes) {
				StartDebounceWindow();
			}
		}
	}

	protected override void CloseTarget() {
		if (_debounceCts?.IsCancellationRequested == false) {
			_debounceCts.Cancel();
			FlushWrappedTarget();
		}
		base.CloseTarget();
	}

	private readonly object _debounceLock = new();

	private CancellationTokenSource? _debounceCts;

	private void StartDebounceWindow() {
		var lockTaken = false;
		try {
			lockTaken = Monitor.TryEnter(_debounceLock, TimeSpan.FromMilliseconds(100));
			if (lockTaken) {
				if (_debounceCts?.IsCancellationRequested == false) {
					return;
				}

				_debounceCts = new();
				var token = _debounceCts.Token;
				var delay = Counter.StartTimestamp + Interval - TimeProvider.GetUtcNow();
				Task.Run(async () => {
					await Task.Delay(delay, TimeProvider, token);

					if (token.IsCancellationRequested) {
						return;
					}

					FlushWrappedTarget();
				}, token);
			} else {
				InternalLogger.Warn("Failed to acquire debounce lock");
			}
		} catch (Exception ex) {
			InternalLogger.Error(ex, "Failed to acquire debounce lock");
		}
		finally {
			if (lockTaken) {
				Monitor.Exit(_debounceLock);
			}
		}
	}

	private void FlushWrappedTarget(AsyncContinuation? continuation = null) {
		continuation ??= ex => InternalLogger.Error(ex, "Failed to flush");
		WrappedTarget.Flush(continuation);
	}
}

