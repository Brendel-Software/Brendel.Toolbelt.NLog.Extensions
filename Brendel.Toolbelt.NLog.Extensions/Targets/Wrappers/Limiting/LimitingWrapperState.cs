namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Representing the state of the <see cref="StatefulLimitingTargetWrapper"/>.
/// </summary>
public class LimitingWrapperState {
	/// <summary>
	/// Timestamp of the first write to the wrapped target.
	/// </summary>
	public DateTime IntervalStartUtc { get; set; }

	/// <summary>
	/// Count of writes to the wrapped target.
	/// </summary>
	public int WriteCount { get; set; }

	/// <summary>
	/// Determines whether the specified time interval has expired since the interval start time.
	/// </summary>
	public bool CheckExpired(TimeSpan interval, DateTime currentUtc) {
		return IntervalStartUtc + interval < currentUtc;
	}

	/// <summary>
	/// Resets the <see cref="IntervalStartUtc"/> and <see cref="WriteCount"/>.
	/// </summary>
	public void Reset() {
		IntervalStartUtc = DateTime.MinValue;
		WriteCount = 0;
	}

	/// <summary>
	/// Increments the <see cref="WriteCount"/> and sets the <see cref="IntervalStartUtc"/> if not already set.
	/// </summary>
	public void UpdateCounter(DateTime currentUtc) {
		if (IntervalStartUtc == DateTime.MinValue) {
			IntervalStartUtc = currentUtc;
		}
		WriteCount += 1;
	}
}