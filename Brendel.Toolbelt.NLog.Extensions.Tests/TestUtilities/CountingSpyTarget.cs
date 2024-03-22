using NLog;
using NLog.Targets;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

[Target("CountingSpy")]
public class CountingSpyTarget : Target {
	public int WrittenMessagesCounter { get; private set; }

	protected override void Write(LogEventInfo _) {
		WrittenMessagesCounter += 1;
	}
}