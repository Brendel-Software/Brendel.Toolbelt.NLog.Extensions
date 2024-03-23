namespace Brendel.Toolbelt.NLog.Extensions.Util.Counter;

/// <summary>
/// Representing the counts within a time interval.
/// </summary>
public class TimestampedCounter {
	/// <summary>
	/// Timestamp of the first count within the interval.
	/// </summary>
	public DateTime StartTimestamp { get; set; }

	/// <summary>
	/// Counts within the time interval.
	/// </summary>
	public int Count { get; set; }

	/// <summary>
	/// Determines whether the given <see cref="interval"/> and <see cref="StartTimestamp"/> have expired in relation to the <paramref name="timestamp"/>
	/// </summary>
	/// <param name="interval">the interval to add to the <see cref="StartTimestamp"/></param>
	/// <param name="timestamp">the timestamp to check against</param>
	/// <returns><c>true</c> if the interval has expired, otherwise <c>false</c></returns>
	public bool CheckExpired(TimeSpan interval, DateTime timestamp) {
		return StartTimestamp + interval < timestamp;
	}

	/// <summary>
	/// Resets the <see cref="StartTimestamp"/> and <see cref="Count"/>.
	/// </summary>
	public void Reset() {
		StartTimestamp = DateTime.MinValue;
		Count = 0;
	}

	/// <summary>
	/// Increments the <see cref="Count"/> and sets the <see cref="StartTimestamp"/> if not already set.
	/// </summary>
	public void Increment(DateTime timestamp) {
		if (StartTimestamp == DateTime.MinValue) {
			StartTimestamp = timestamp;
		}
		Count += 1;
	}
}