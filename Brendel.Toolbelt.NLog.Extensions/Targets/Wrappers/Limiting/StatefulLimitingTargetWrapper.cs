using NLog;
using NLog.Common;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// A delegate that provides the current UTC date and time.
/// </summary>
/// <returns>The current UTC date and time as a <see cref="DateTime"/>.</returns>
/// <remarks>
/// This delegate is used to abstract the access to the current UTC time,
/// allowing for more flexible implementations, such as during testing where
/// the current UTC time might need to be mocked or set to a specific moment.
/// </remarks>
public delegate DateTime CurrentUtcTimeProvider();

[Target("StatefulLimitingWrapper", IsWrapper = true)]
public class StatefulLimitingTargetWrapper : WrapperTargetBase {
	protected LimitingWrapperState State { get; set; } = new();

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
	public CurrentUtcTimeProvider TimeProvider { get; set; } = () => DateTime.UtcNow;

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

		if (State.CheckExpired(interval, TimeProvider.Invoke())) {
			State.Reset();
		}

		var limit = RenderLogEvent(MessageLimit, logEvent.LogEvent);
		if (State.WriteCount < limit) {
			WrappedTarget.WriteAsyncLogEvent(logEvent);
			State.UpdateCounter(TimeProvider.Invoke());
			return;
		}

		logEvent.Continuation(null);
		InternalLogger.Trace($"{{0}}: {nameof(MessageLimit)}={{1}} within {nameof(Interval)}={{2}} reached discarded logEvent", this, MessageLimit, Interval);
	}
}