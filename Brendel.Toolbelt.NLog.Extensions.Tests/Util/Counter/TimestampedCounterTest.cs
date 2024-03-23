using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using JetBrains.Annotations;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Counter;

[TestSubject(typeof(TimestampedCounter))]
public class TimestampedCounterTest {
	[Fact]
	public void CheckExpired_returns_true_on_expired_interval() {
		var sut = new TimestampedCounter();
		var now = DateTime.UtcNow;
		var interval = TimeSpan.FromMinutes(5);

		sut.StartTimestamp = now - TimeSpan.FromMinutes(6);
		var result = sut.CheckExpired(interval, now);

		Assert.True(result);
	}

	[Fact]
	public void CheckExired_returns_false_on_active_interval() {
		var now = DateTime.UtcNow;
		var interval = TimeSpan.FromMinutes(5);

		var sut = new TimestampedCounter();
		sut.StartTimestamp = now - TimeSpan.FromMinutes(4);

		var result = sut.CheckExpired(interval, now);

		Assert.False(result);
	}

	[Fact]
	public void Reset_resets_state() {
		var state = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow,
			Count = 3
		};

		state.Reset();

		Assert.Equal(DateTime.MinValue, state.StartTimestamp);
		Assert.Equal(0, state.Count);
	}

	[Fact]
	public void Increment_increments_write_count() {
		var state = new TimestampedCounter {
			Count = 3
		};

		state.Increment(DateTime.UtcNow);

		Assert.Equal(4, state.Count);
	}

	[Fact]
	public void Increment_sets_interval_start_on_resetted_state() {
		var sut = new TimestampedCounter();
		sut.Reset();

		var now = DateTime.UtcNow;
		sut.Increment(now);

		Assert.Equal(now, sut.StartTimestamp);
	}
}