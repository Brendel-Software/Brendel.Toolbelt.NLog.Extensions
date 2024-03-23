using NLog;
using NLog.Common;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities.Targets;

[Target("CountingSpy")]
public class CountingSpyTarget : Target {
	public int WrittenMessagesCounter { get; private set; }

	public int FlushOperationsCounter { get; private set; }

	protected override void Write(LogEventInfo eventInfo) {
		WrittenMessagesCounter += 1;
		base.Write(eventInfo);
	}

	protected override void FlushAsync(AsyncContinuation asyncContinuation) {
		FlushOperationsCounter += 1;
		base.FlushAsync(asyncContinuation);
	}
}