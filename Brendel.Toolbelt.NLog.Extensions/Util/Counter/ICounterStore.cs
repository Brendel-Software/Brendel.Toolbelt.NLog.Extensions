namespace Brendel.Toolbelt.NLog.Extensions.Util.Counter;

/// <summary>
/// Saves and loads a <see cref="TimestampedCounter"/>.
/// </summary>
public interface ICounterStore {
	/// <summary>
	/// Loads the state from the store
	/// </summary>
	/// <returns><c>null</c> when no state was stored</returns>
	public TimestampedCounter? LoadState();

	/// <summary>
	/// Saves the state to the store
	/// </summary>
	public void SaveState(TimestampedCounter state);

	/// <summary>
	/// Deletes the state from the store
	/// </summary>
	public void DeleteState();
}