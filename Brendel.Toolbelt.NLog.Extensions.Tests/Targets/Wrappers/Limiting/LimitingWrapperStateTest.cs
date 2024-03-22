using Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;
using JetBrains.Annotations;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.Targets.Wrappers.Limiting;

[TestSubject(typeof(LimitingWrapperState))]
public class LimitingWrapperStateTest {

	[Fact]
	public void CheckExpired_returns_true_on_expired_interval() {
		var sut = new LimitingWrapperState();
		var now = DateTime.UtcNow;
		var interval = TimeSpan.FromMinutes(5);

		sut.IntervalStartUtc = now - TimeSpan.FromMinutes(6);
		var result = sut.CheckExpired(interval, now);

		Assert.True(result);
	}

	[Fact]
	public void CheckExired_returns_false_on_active_interval() {
		var now = DateTime.UtcNow;
		var interval = TimeSpan.FromMinutes(5);

		var sut = new LimitingWrapperState();
		sut.IntervalStartUtc = now - TimeSpan.FromMinutes(4);

		var result = sut.CheckExpired(interval, now);

		Assert.False(result);
	}

	[Fact]
	public void Reset_resets_state() {
		var state = new LimitingWrapperState {
			IntervalStartUtc = DateTime.UtcNow,
			WriteCount = 3
		};

		state.Reset();

		Assert.Equal(DateTime.MinValue, state.IntervalStartUtc);
		Assert.Equal(0, state.WriteCount);
	}

	[Fact]
	public void UpdateCounter_increments_write_count() {
		var state = new LimitingWrapperState {
			WriteCount = 3
		};

		state.UpdateCounter(DateTime.UtcNow);

		Assert.Equal(4, state.WriteCount);
	}

	[Fact]
	public void UpdateCounter_sets_interval_start_on_resetted_state() {
		var sut = new LimitingWrapperState();
		sut.Reset();

		var now = DateTime.UtcNow;
		sut.UpdateCounter(now);

		Assert.Equal(now, sut.IntervalStartUtc);
	}
}