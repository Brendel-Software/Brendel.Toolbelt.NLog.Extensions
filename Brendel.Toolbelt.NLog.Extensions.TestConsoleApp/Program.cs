using System.Diagnostics;
using NLog;

var logger = LogManager.Setup().GetCurrentClassLogger();
try {
	logger.Trace("Trace!");
	logger.Debug("Debug!");
	logger.Info("Info!");
	logger.Trace("Trace!");
	logger.Debug("Debug!");
	logger.Info("Info!");
	logger.Trace("Trace!");
	throw new InvalidOperationException("Ich bin ein Test");
	logger.Debug("Debug!");
	logger.Info("Info!");
	logger.Trace("Trace!");
	logger.Debug("Debug!");
	logger.Info("Info!");
	logger.Warn("Warnung!");
} catch (Exception ex) {
	logger.Error(ex, "Fehler!");
}