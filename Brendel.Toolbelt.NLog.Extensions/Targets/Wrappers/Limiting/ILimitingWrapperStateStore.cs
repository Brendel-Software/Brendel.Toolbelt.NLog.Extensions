namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Saves and loads a <see cref="LimitingWrapperState"/>.
/// </summary>
public interface ILimitingWrapperStateStore {
	/// <summary>
	/// Loads the state from the store
	/// </summary>
	/// <returns><c>null</c> when no state was stored</returns>
	public LimitingWrapperState? LoadState();

	/// <summary>
	/// Saves the state to the store
	/// </summary>
	public void SaveState(LimitingWrapperState state);

	/// <summary>
	/// Deletes the state from the store
	/// </summary>
	public void DeleteState();
}