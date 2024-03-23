using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using JetBrains.Annotations;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Util.Counter;

[TestSubject(typeof(TimestampedCounterExtensions))]
public class TimestampedCounterExtensionsTest {
	[Fact]
	public void CanIncrement_returns_true_on_expired_counter() {
		var sut = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow - TimeSpan.FromMinutes(6),
			Count = 500
		};

		var result = sut.CanIncrement(TimeSpan.FromMinutes(5), DateTime.UtcNow, 5);

		Assert.True(result);
	}

	[Fact]
	public void CanIncrement_returns_true_when_limit_not_reached() {
		var sut = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow,
			Count = 3
		};

		var result = sut.CanIncrement(TimeSpan.FromMinutes(5), DateTime.UtcNow, 5);

		Assert.True(result);
	}

	[Fact]
	public void CanIncrement_returns_false_when_limit_reached() {
		var sut = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow,
			Count = 5
		};

		var result = sut.CanIncrement(TimeSpan.FromMinutes(5), DateTime.UtcNow, 5);

		Assert.False(result);
	}

	[Fact]
	public void IncrementIntervalAware_resets_counter_on_expired_interval() {
		var sut = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow - TimeSpan.FromMinutes(6),
			Count = 500
		};

		sut.IncrementIntervalAware(TimeSpan.FromMinutes(5), DateTime.UtcNow);

		Assert.Equal(1, sut.Count);
	}

	[Fact]
	public void IncrementIntervalAware_does_not_reset_counter_on_active_interval() {
		var sut = new TimestampedCounter {
			StartTimestamp = DateTime.UtcNow - TimeSpan.FromMinutes(4),
			Count = 500
		};

		sut.IncrementIntervalAware(TimeSpan.FromMinutes(5), DateTime.UtcNow);

		Assert.Equal(501, sut.Count);
	}
}