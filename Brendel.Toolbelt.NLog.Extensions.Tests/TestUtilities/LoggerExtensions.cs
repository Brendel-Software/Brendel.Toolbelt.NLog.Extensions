using NLog;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

internal static class LoggerExtensions {
	public static void WriteFakeDebugMessages(this Logger logger, int count) {
		for (var i = 0; i < count; i++) {
			logger.Debug(Guid.NewGuid().ToString());
		}
	}
}