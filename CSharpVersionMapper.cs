using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CSharpPatchDroid;

public static class CSharpVersionMapper
{
    private static readonly Dictionary<string, string> frameworkToCSharpVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // 简单写法，版本号都去点，比如 v35 = v3.5
        { ".netframework,version=v20", "2.0" },
        { ".netframework,version=v30", "2.0" },
        { ".netframework,version=v35", "3.0" },
        { ".netframework,version=v40", "4.0" },
        { ".netframework,version=v45", "5.0" },
        { ".netframework,version=v46", "6.0" },
        { ".netframework,version=v47", "7.0" },
        { ".netframework,version=v472", "7.3" },
        { ".netframework,version=v48", "7.3" },

        { ".netcoreapp,version=v10", "7.0" },
        { ".netcoreapp,version=v20", "7.1" },
        { ".netcoreapp,version=v30", "8.0" },
        { ".netcoreapp,version=v31", "8.0" },
        { ".netcoreapp,version=v50", "9.0" },
        { ".netcoreapp,version=v60", "10.0" },
        { ".netcoreapp,version=v70", "11.0" },
        { ".netcoreapp,version=v80", "12.0" },

        { ".netstandard,version=v10", "5.0" },
        { ".netstandard,version=v13", "6.0" },
        { ".netstandard,version=v20", "7.3" },
        { ".netstandard,version=v21", "8.0" },
    };

    /// <summary>
    /// 完全标准化 TargetFramework 字符串，去除所有空白、Profile，括号等，
    /// 并把版本号里的点去掉，方便匹配。
    /// </summary>
    private static string NormalizeFrameworkString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. 去除所有空白
        var s = Regex.Replace(input, @"\s+", "");

        // 2. 去掉 Profile=xxx
        s = Regex.Replace(s, @",profile=[^,)\]]+", "", RegexOptions.IgnoreCase);

        // 3. 去掉所有中括号和小括号
        s = s.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");

        // 4. 转小写
        s = s.ToLowerInvariant();

        // 5. 去掉版本号中所有点号，例如v4.7.2 -> v472，v3.1 -> v31
        s = Regex.Replace(s, @"version=v(\d+)\.(\d+)\.(\d+)", m => $"version=v{m.Groups[1].Value}{m.Groups[2].Value}{m.Groups[3].Value}");
        s = Regex.Replace(s, @"version=v(\d+)\.(\d+)", m => $"version=v{m.Groups[1].Value}{m.Groups[2].Value}");

        return s;
    }

    public static string GetMaxCSharpVersion(string targetFramework)
    {
        var normalized = NormalizeFrameworkString(targetFramework);

        if (string.IsNullOrEmpty(normalized))
            return "未知";

        if (frameworkToCSharpVersion.TryGetValue(normalized, out var version))
            return version;

        // 尝试部分匹配（忽略尾部额外内容）
        foreach (var kvp in frameworkToCSharpVersion)
        {
            if (normalized.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return "未知";
    }
}
