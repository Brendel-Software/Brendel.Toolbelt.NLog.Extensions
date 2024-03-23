using Brendel.Toolbelt.NLog.Extensions.Util.Counter;
using NLog.Common;

namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Wraps another target and limits the number of messages written to it per interval.
/// The state of the wrapper is persisted to a <see cref="ITimestampedCounterStore"/> interface on close and restored on startup.
/// </summary>
public abstract class PersistentLimitingTargetWrapperBase : StatefulLimitingTargetWrapper {
	protected abstract ITimestampedCounterStore Store { get; }

	protected override void InitializeTarget() {
		Counter = LoadState();
		base.InitializeTarget();
	}

	protected override void CloseTarget() {
		SaveStateToFile();
		base.CloseTarget();
	}

	/// <summary>
	/// Saves the <see cref="_state"/> to the <see cref="Store"/>.
	/// </summary>
	private void SaveStateToFile() {
		try {
			Store.SaveState(Counter);
			InternalLogger.Debug("{0}: saved state", this);
		} catch (Exception e) {
			InternalLogger.Error(e, "{0}: failed to save state", this);
		}
	}

	/// <summary>
	/// Loads the <see cref="TimestampedCounter"/> from the <see cref="Store"/>
	/// </summary>
	/// <returns></returns>
	private TimestampedCounter LoadState() {
		try {
			if (Store.LoadState() is { } state) {
				InternalLogger.Debug("{0}: loaded state", this);
				return state;
			}
		} catch (Exception e) {
			InternalLogger.Error(e, "{0}: failed to load state", this);
		}

		InternalLogger.Trace("{0}: created new state", this);
		return new();
	}
}