namespace Brendel.Toolbelt.NLog.Extensions.Util.Counter;

public static class TimestampedCounterExtensions {
	/// <summary>
	/// Resets the counter if it has expired and then increments it.
	/// </summary>
	/// <param name="counter">the counter to increment</param>
	/// <param name="interval">the interval to check against</param>
	/// <param name="timestamp">the timestamp to check against</param>
	public static void IncrementIntervalAware(this TimestampedCounter counter, TimeSpan interval, DateTimeOffset timestamp) {
		if (counter.CheckExpired(interval, timestamp.UtcDateTime)) {
			counter.Reset();
		}

		counter.Increment(timestamp.UtcDateTime);
	}

	/// <summary>
	/// Checks if the counter can be incremented.
	/// </summary>
	/// <param name="counter">the counter to check</param>
	/// <param name="inverval">the interval to check against</param>
	/// <param name="timestamp">the timestamp to check against</param>
	/// <param name="limit">the limit to check against</param>
	public static bool CanIncrement(this TimestampedCounter counter, TimeSpan inverval, DateTimeOffset timestamp, int limit) {
		if (counter.CheckExpired(inverval, timestamp.UtcDateTime)) {
			return true;
		}

		return counter.Count < limit;
	}
}