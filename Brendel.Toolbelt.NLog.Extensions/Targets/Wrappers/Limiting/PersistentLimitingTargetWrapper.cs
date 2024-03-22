using System.Reflection;
using NLog;
using NLog.Layouts;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// Wraps another target and limits the number of messages written to it per interval.
/// The state of the wrapper is persisted to a file on close and restored on startup.
/// </summary>
[Target("PersistentLimitingWrapper", IsWrapper = true)]
public class PersistentLimitingWrapper : PersistentLimitingTargetWrapperBase {
	private ILimitingWrapperStateStore? _store;
	private string? _stateCacheFile;

	protected override ILimitingWrapperStateStore Store => _store ??= CreateStore();

	/// <summary>
	/// File for storing the <see cref="LimitingWrapperState"/>.<br/>
	/// <br/>
	/// When empty the PersistentLimitingTargetWrapper tries to create a <i>state file</i> prefixed with the <see cref="Assembly.GetExecutingAssembly"/>
	/// and the <see cref="Target.Name"/> within Directory acquired by <i><see cref="Path.GetTempPath"/></i>.
	/// </summary>
	/// <remarks>
	/// This Property will only be evaluated once during Target initialization.
	/// </remarks>
	public Layout<string?> StateCacheFile { get; set; } = new(null);

	protected override void InitializeTarget() {
		_stateCacheFile = RenderLogEvent(StateCacheFile, LogEventInfo.CreateNullEvent());
		base.InitializeTarget();
	}

	private LimitingWrapperStateJsonFileStore CreateStore() {
		var builder = new LimitingWrapperStateJsonFileStore.Builder();

		if (string.IsNullOrWhiteSpace(_stateCacheFile)) {
			builder.UseTargetName(this);
		} else {
			builder.File = _stateCacheFile;
		}

		return builder.Build();
	}
}