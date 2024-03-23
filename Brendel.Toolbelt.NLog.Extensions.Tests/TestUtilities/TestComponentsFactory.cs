using NLog;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Brendel.Toolbelt.NLog.Extensions.Tests.TestUtilities;

public static class TestComponentsFactory {
	public static (Logger, TWrapper, TWrappedTarget) BuildWrapperTestComponentsFromXml<TWrapper, TWrappedTarget>(string xml, string? wrapperName = "wrapper", string? targetName = null)
		where TWrapper : WrapperTargetBase
		where TWrappedTarget : Target {
		var logFactory = new LogFactory().Setup().LoadConfigurationFromXml(xml).LogFactory;
		var wrapper = (TWrapper) logFactory.Configuration.FindTargetByName(wrapperName);
		var target = targetName is not null
						 ? (TWrappedTarget) logFactory.Configuration.FindTargetByName(targetName)
						 : (TWrappedTarget) wrapper.WrappedTarget;
		var logger = logFactory.GetCurrentClassLogger();

		return (logger, wrapper, target);
	}
}