using System.Diagnostics;
using NLog;

try {
	var logger = LogManager.Setup().GetCurrentClassLogger();
	var config = LogManager.Configuration;
	logger.Trace("Trace!");
	logger.Debug("Debug!");
	logger.Info("Info!");
	logger.Warn("Warnung!");
} catch (Exception ex) {
	Debugger.Break();
}