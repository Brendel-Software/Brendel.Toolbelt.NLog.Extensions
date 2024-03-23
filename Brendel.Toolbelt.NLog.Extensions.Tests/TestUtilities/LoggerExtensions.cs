using NLog;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

internal static class LoggerExtensions {
	public static void WriteFakeDebugMessages(this Logger logger, int count)=> logger.WriteFakeMessages(count, LogLevel.Debug);

	public static void WriteFakeWarnMessages(this Logger logger, int count) => logger.WriteFakeMessages(count, LogLevel.Warn);

	public static void WriteFakeMessages(this Logger logger, int count, LogLevel level) {
		for (var i = 0; i < count; i++) {
			logger.Log(level, Guid.NewGuid().ToString());
		}
	}
}