namespace Brendel.Toolbelt.NLog.Extensions.Targets.Wrappers.Limiting;

/// <summary>
/// A delegate that provides the current UTC date and time.
/// </summary>
/// <returns>The current UTC date and time as a <see cref="DateTime"/>.</returns>
/// <remarks>
/// This delegate is used to abstract the access to the current UTC time,
/// allowing for more flexible implementations, such as during testing where
/// the current UTC time might need to be mocked or set to a specific moment.
/// </remarks>
public delegate DateTime CurrentUtcTimeProvider();