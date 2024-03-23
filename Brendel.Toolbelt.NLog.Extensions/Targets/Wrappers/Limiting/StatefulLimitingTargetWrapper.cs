using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using NLog;
using NLog.Common;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Wraps another target and limits the number of messages written to it per interval.
/// </summary>
[Target("StatefulLimitingWrapper", IsWrapper = true)]
public class StatefulLimitingTargetWrapper : WrapperTargetBase {
	protected TimestampedCounter Counter { get; set; } = new();

	/// <summary>
	/// Gets or sets the maximum allowed number of <see cref="LogEventInfo"/>s written to the <see cref="WrapperTargetBase.WrappedTarget"/> per <see cref="Interval" />.
	/// </summary>
	/// <remarks>
	/// Messages received after <see cref="MessageLimit" /> has been reached in the current <see cref="Interval" /> will be discarded.
	/// </remarks>
	public Layout<int> MessageLimit { get; set; } = new(0);

	/// <summary>
	/// Gets or sets the interval in which <see cref="LogEventInfo"/>s will be written to the <see cref="WrapperTargetBase.WrappedTarget"/> up to the <see cref="MessageLimit" />.
	/// </summary>
	/// <remarks>
	/// Messages received after <see cref="MessageLimit" /> has been reached in the current <see cref="Interval" /> will be discarded.
	/// </remarks>
	public Layout<TimeSpan> Interval { get; set; } = new(TimeSpan.Zero);

	/// <summary>
	/// A delegate that provides the current UTC date and time.
	/// </summary>
	public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

	protected override void InitializeTarget() {
		if (MessageLimit.IsFixed && MessageLimit.FixedValue <= 0) {
			throw new NLogConfigurationException($"{nameof(MessageLimit)} property must be > 0");
		}

		if (Interval.IsFixed && Interval.FixedValue <= TimeSpan.Zero) {
			throw new NLogConfigurationException($"{nameof(Interval)} property must be > 0");
		}

		InternalLogger.Trace($"{{0}}: Initialized with {nameof(MessageLimit)}={{1}} and {nameof(Interval)}={{2}}", this, MessageLimit, Interval);

		base.InitializeTarget();
	}

	protected override void Write(AsyncLogEventInfo logEvent) {
		var interval = RenderLogEvent(Interval, logEvent.LogEvent);
		var limit = RenderLogEvent(MessageLimit, logEvent.LogEvent);

		if (Counter.CanIncrement(interval, TimeProvider.GetUtcNow(), limit)) {
			WrappedTarget.WriteAsyncLogEvent(logEvent);
			Counter.IncrementIntervalAware(interval, TimeProvider.GetUtcNow());
		} else {
			logEvent.Continuation(null);
			InternalLogger.Trace($"{{0}}: {nameof(MessageLimit)}={{1}} within {nameof(Interval)}={{2}} reached discarded logEvent", this, MessageLimit, Interval);
		}
	}
}