using System.Text.RegularExpressions;

namespace Revalidate;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"(\w+)\s+date=(\d{4}-\d{2}-\d{2}_\d{2}_\d{2})\s+(Svn=(\d+))?(git=([^\s]+))?\s+GameVersion=([\d.]+)")]
    public static partial Regex ExeVersionRegex();
}
