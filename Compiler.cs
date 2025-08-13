using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSharpPatchDroid;
public static class Compiler
{
    public static bool Start(string csprojPath, string outputPath, string rootDir = null)
    {
        //Console.Write(csprojPath);
        string baseDir;
        if (rootDir == null)
        {
            baseDir = Path.GetDirectoryName(csprojPath);
        }
        else
        {
            baseDir = rootDir;
        }
        var doc = XDocument.Load(csprojPath);

        // 1. 读取属性
        var props = doc.Descendants("PropertyGroup")
                       .SelectMany(pg => pg.Elements())
                       .ToDictionary(e => e.Name.LocalName, e => e.Value);

        var allowUnsafe = props.TryGetValue("AllowUnsafeBlocks", out var unsafeVal) && unsafeVal.Equals("true", StringComparison.OrdinalIgnoreCase);
        var langVersion = props.ContainsKey("LangVersion") ? props["LangVersion"] : "latest";

        // 2. 读取引用
        //var baseDir = Path.GetDirectoryName(csprojPath);

        var references = doc.Descendants("Reference")
            .Select(r => r.Element("HintPath")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path =>
            {
                var absPath = Path.GetFullPath(Path.Combine(baseDir ?? ".", path));
                return MetadataReference.CreateFromFile(absPath);
            })
            .ToList();



        // 3. 收集源文件（支持 **/*.cs）

        var compileItems = doc.Descendants("Compile")
            .Select(c => c.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
        List<string> compileItems2 = new List<string>();
        foreach (var i in compileItems)
        {
            if (IsWildcard(i))
            {
                List<string> files = GetFilesByPattern(i, csprojPath, baseDir);
                foreach (var f in files)
                {
                    compileItems2.Add(f);

                }
            }
            else
            {
                compileItems2.Add(i);



            }
        }



        // 解析语言版本
        LanguageVersion languageVersion;
        //Console.WriteLine(Program.GetLangVersionNormal(langVersion));

        if (Enum.TryParse(Program.GetLangVersionNormal(langVersion), ignoreCase: true, out LanguageVersion languagev))
        {
            languageVersion = languagev;

        }
        else
        {
            languageVersion = LanguageVersion.Latest;

        }
        //Console.WriteLine(languageVersion.ToString());
        // 4. 编译
        var syntaxTrees = compileItems2.Select(file =>
            CSharpSyntaxTree.ParseText(
                File.ReadAllText(file),
                new CSharpParseOptions(languageVersion: languageVersion),
                path: file
            )
        );

        var compilation = CSharpCompilation.Create(
            assemblyName: props.ContainsKey("AssemblyName") ? props["AssemblyName"] : "OutputAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: allowUnsafe
            )
        );

        Directory.CreateDirectory(baseDir);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        string tempPath = outputPath + ".tmp";
        var result = compilation.Emit(tempPath);

        stopwatch.Stop();
        var time = stopwatch.Elapsed.TotalSeconds.ToString();
        int warningCount;
        int errorCount;
        int infoCount;
        int hiddenCount;
        if (!result.Success)
        {
            File.Delete(tempPath);
            Print.error("编译失败：");
            Program.log.error("编译失败", "Compiler,Start Line110");
            var newReferences = new List<ErrorAnalysis.Reference>();
            File.WriteAllText("/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt", string.Empty);
            warningCount = 0;
            errorCount = 0;
            infoCount = 0;
            hiddenCount = 0;
            foreach (var diag in result.Diagnostics)
            {
                ErrorAnalysis errorAnalysis = new ErrorAnalysis(diag);
                if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    warningCount++;
                }
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    errorCount++;
                }
                if (diag.Severity == DiagnosticSeverity.Info)
                {
                    infoCount++;
                }
                if (diag.Severity == DiagnosticSeverity.Hidden)
                {
                    hiddenCount++;
                }
                if (diag.Id == "CS0012")
                {
                    ErrorAnalysis.Reference reference = errorAnalysis.getReference();
                    newReferences.Add(reference);
                }


                File.AppendAllText("/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt", diag.ToString() + Environment.NewLine);

            }
            if (errorCount == 0)
            {
                Print.sucess($"错误数量:{errorCount}");

            }
            else
            {
                Print.error($"错误数量:{errorCount}");
            }
            if (warningCount == 0)
            {
                Print.sucess($"警告数量:{warningCount}");

            }
            else
            {
                Print.warning($"警告数量:{warningCount}");
            }
            if (infoCount == 0)
            {
                Print.sucess($"通知数量:{infoCount}");

            }
            else
            {
                Print.print($"通知数量:{infoCount}");
            }

            if (hiddenCount == 0)
            {
                Print.sucess($"隐藏数量:{hiddenCount}");

            }
            else
            {
                Print.print($"隐藏数量:{hiddenCount}");
            }
            Print.print("已将日志输出到/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt");
            if (Program.settings.autoComplete)
            {
                var check = new List<string>();
                var rightReferences = new List<ErrorAnalysis.Reference>();
                var allDll = Directory.GetFiles(Path.GetDirectoryName(baseDir)).ToList();
                allDll.RemoveAll(file => !file.EndsWith(".dll"));
                Print.print("开始纠错");
                if (Program.settings.autoCompleteReference)
                {
                    Print.print("正在检查缺少依赖");
                    foreach (var i in newReferences)
                    {
                        if (check.Contains(i.include))
                        {
                            continue;
                        }
                        if (allDll.Contains(Path.Join(Path.GetDirectoryName(baseDir), i.include + ".dll")))
                        {
                            check.Add(i.include);
                            var rightReference = new ErrorAnalysis.Reference(i.include, i.version, i.culture, i.publicKeyToken);
                            rightReference.setHintPath(Path.Join(Path.GetDirectoryName(baseDir), i.include + ".dll"));
                            rightReferences.Add(rightReference);
                            Print.print($"缺少依赖:{i.include}");
                        }
                    }
                    Print.print("开始向项目父目录下寻找依赖");
                    foreach (var i in rightReferences)
                    {
                        Print.sucess($"依赖已找到，已自动补齐{i.include}:{i.hintPath}");
                        XDocument doc2 = XDocument.Load(csprojPath);

                        // 创建新的 Reference 元素
                        XElement newReference = new XElement("Reference",
                            new XAttribute("Include", i.include),
                            new XElement("HintPath", Path.GetFullPath(Path.Combine(baseDir ?? ".", i.hintPath)))
                        );

                        // 查找 ItemGroup 元素并添加新的 Reference
                        XElement itemGroup = doc2.Descendants("ItemGroup")
                            .FirstOrDefault(ig => ig.Elements("Reference").Any());
                        if (itemGroup != null)
                        {
                            itemGroup.Add(newReference);
                        }
                        else
                        {
                            // 如果没有找到 ItemGroup，则创建一个新的 ItemGroup
                            doc.Root.Add(new XElement("ItemGroup", newReference));
                        }

                        // 保存修改后的 .csproj 文件
                        doc2.Save(csprojPath);

                        Print.sucess("已更新.csproj文件，可以再试一次");


                    }
                }
            }

        }
        else
        {
            File.Replace(tempPath, outputPath, null);
            File.WriteAllText("/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt", string.Empty);
            warningCount = 0;
            errorCount = 0;
            hiddenCount = 0;
            infoCount = 0;
            foreach (var diag in result.Diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    warningCount++;

                }
                if (diag.Severity == DiagnosticSeverity.Info)
                {
                    infoCount++;
                }
                if (diag.Severity == DiagnosticSeverity.Hidden)
                {
                    hiddenCount++;
                }
                File.AppendAllText("/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt", diag.ToString() + Environment.NewLine);
            }
            Print.sucess($"编译成功:{outputPath}");
            if (errorCount == 0)
            {
                Print.sucess($"错误数量:{errorCount}");

            }
            else
            {
                Print.error($"错误数量:{errorCount}");
            }
            if (warningCount == 0)
            {
                Print.sucess($"警告数量:{warningCount}");

            }
            else
            {
                Print.warning($"警告数量:{warningCount}");
            }
            if (infoCount == 0)
            {
                Print.sucess($"通知数量:{infoCount}");

            }
            else
            {
                Print.print($"通知数量:{infoCount}");
            }

            if (hiddenCount == 0)
            {
                Print.sucess($"隐藏数量:{hiddenCount}");

            }
            else
            {
                Print.print($"隐藏数量:{hiddenCount}");
            }
            Print.sucess($"本次用时:{time}秒");
            Print.print("已将日志输出到/storage/emulated/0/CSharpPatchDroidOutput/buildLog.txt");


            Program.log.ok("编译成功:");
        }


        return result.Success;
    }
    public static List<string> GetFilesByPattern(string pattern, string csprojPath, string baseDir)
    {
        var projectDir = baseDir ?? ".";
        var results = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
            return results;

        // 先拆分目录和文件模式
        // 例如 "**/*.cs" => directoryPattern="**", filePattern="*.cs"
        var lastSlash = pattern.LastIndexOfAny(new[] { '\\', '/' });
        string directoryPattern, filePattern;
        if (lastSlash < 0)
        {
            directoryPattern = ".";
            filePattern = pattern;
        }
        else
        {
            directoryPattern = pattern.Substring(0, lastSlash);
            filePattern = pattern.Substring(lastSlash + 1);
        }

        // 递归匹配目录
        var matchedDirs = MatchDirectories(projectDir, directoryPattern);

        // 匹配目录下文件
        foreach (var dir in matchedDirs)
        {
            try
            {
                var files = Directory.GetFiles(dir, filePattern, SearchOption.TopDirectoryOnly);
                results.AddRange(files);
            }
            catch
            {
                Program.log.error("没有找到文件", "Compiler,GetFilesByPattern Line160");
                // 忽略访问异常
            }
        }
        return results.Distinct().ToList();
    }
    private static List<string> MatchDirectories(string baseDir, string directoryPattern)
    {
        var dirs = new List<string>();

        if (string.IsNullOrEmpty(directoryPattern) || directoryPattern == ".")
        {
            dirs.Add(baseDir);
            return dirs;
        }

        // 处理 ** 递归
        if (directoryPattern.StartsWith("**"))
        {
            // 去掉开头的 ** 和可能的斜杠
            var rest = directoryPattern.Length > 2 && (directoryPattern[2] == '/' || directoryPattern[2] == '\\')
                ? directoryPattern.Substring(3)
                : directoryPattern.Substring(2);

            // 所有子目录
            var allSubDirs = Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories).ToList();

            // 包含当前目录
            allSubDirs.Add(baseDir);

            if (string.IsNullOrEmpty(rest))
            {
                // 只有 **
                return allSubDirs;
            }
            else
            {
                // 递归匹配剩余目录
                var matched = new List<string>();
                foreach (var subDir in allSubDirs)
                {
                    matched.AddRange(MatchDirectories(subDir, rest));
                }
                return matched.Distinct().ToList();
            }
        }

        // 没有 ** ，就用简单通配符匹配一级目录
        // 例如 "SpecialFolder"
        // 支持 * ? 等通配符
        try
        {
            var dirsHere = Directory.GetDirectories(baseDir, directoryPattern, SearchOption.TopDirectoryOnly);
            dirs.AddRange(dirsHere);
        }
        catch
        {
            Program.log.error("没有找到路径", "Compiler,GetFilesByPattern Line217");
            // 忽略异常
        }

        return dirs;
    }
    public static bool IsWildcard(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        return pattern.IndexOfAny(new char[] { '*', '?' }) >= 0;
    }

}