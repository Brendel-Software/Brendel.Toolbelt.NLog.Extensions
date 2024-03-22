using System.Text.RegularExpressions;

namespace Brendel.Toolbelt.NLog.Extensions;

public static partial class CompiledExpressions {
	[GeneratedRegex("[^a-zA-Z0-9_-]", RegexOptions.Compiled)]
	public static partial Regex UnsafeFileNameCharacters();
}