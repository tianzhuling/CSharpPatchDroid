using System;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
namespace CSharpPatchDroid;

public class Decompiler
{
    public string dllPath;
    public LanguageVersion lv;
    public string outputDir;
    public string TargetFramework;
    public Decompiler(string dllPath, LanguageVersion lv, string outputDir, string targetFramework)
    {
        Print.print("开始反编译");
        Print.print("配置中……");
        var moduleDefinition = new PEFile(dllPath);
        using var projectFileWriter = new StringWriter();
        using var cts = new CancellationTokenSource();



        var settings = new DecompilerSettings();

        var resolver = new UniversalAssemblyResolver(
            dllPath,
            throwOnError: false,
            targetFramework: targetFramework
        );
        resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath));



        var projectDecompiler = new WholeProjectDecompiler(resolver);
        projectDecompiler.Settings.SetLanguageVersion(lv);
        // 反编译整个项目（DLL）到指定目录，拆分成多个文件
        Print.sucess("配置成功");
        Print.print("反编译中……");
        projectDecompiler.DecompileProject(moduleDefinition, outputDir, projectFileWriter, cts.Token);
        Print.print("");
        Print.sucess("反编译成功");
        Print.print("配置.csproj的基本信息中");
        this.dllPath = dllPath;
        this.TargetFramework = targetFramework;
        this.outputDir = outputDir;
        string csprojPath = Path.Join(this.outputDir, Path.GetFileName(this.outputDir.TrimEnd("/"))) + ".csproj";
        string assemblyName = Path.GetFileName(this.outputDir.TrimEnd("/"));
        string csprojTargetFramework;
        string fw = this.TargetFramework.Split(",")[0].Trim();
        string fwv = this.TargetFramework.Split("Version=v")[1].Trim();
        if (fw.ToUpper() == ".NETCOREAPP")
        {
            csprojTargetFramework = "netcoreapp" + fwv;
        }
        else
        {
            if (fw.ToUpper() == ".NETFRAMEWORK")
            {
                if (float.Parse(fwv) < 5)
                {
                    csprojTargetFramework = "net" + fwv.Replace(".", "");

                }
                else
                {
                    csprojTargetFramework = "net" + fwv;
                }

            }
            else
            {
                if (fw.ToUpper() == ".NETSTANDARD")
                {
                    csprojTargetFramework = "netstandard" + fwv;

                }
                else
                {
                    if (fw.ToUpper() == "SILVERLIGHT")
                    {
                        csprojTargetFramework = "sl" + fwv;
                    }
                    else
                    {
                        csprojTargetFramework = "net8.0";
                    }
                }
            }
        }
        var langVersion = GetLangVersionString(lv);
        var references = moduleDefinition.Metadata.AssemblyReferences;
        var metadataReader = moduleDefinition.Metadata;
        var searchDirs = new[] { Path.GetDirectoryName(dllPath) };
        var allowUnsafe = true;
        var csprojReferences = new List<(string Include, string HintPath)>();

        foreach (var referenceHandle in references)
        {
            var reference = metadataReader.GetAssemblyReference(referenceHandle);
            string referenceName = metadataReader.GetString(reference.Name);

            // 简单假设依赖DLL就在同目录，文件名 = referenceName + ".dll"
            string possiblePath = Path.Combine(searchDirs[0], referenceName + ".dll");

            if (File.Exists(possiblePath))
            {
                csprojReferences.Add((referenceName, possiblePath));
                //Print.print($"依赖: {referenceName}\n路径: {possiblePath}");
            }
            else
            {
                Print.warning($"依赖: {referenceName}\n未找到对应文件");
            }
        }



        WriteCsproj.Start(
            csprojPath,
            assemblyName,
         csprojTargetFramework,
           langVersion,
           allowUnsafe,
        csprojReferences);

    }

    string GetLangVersionString(LanguageVersion version)
    {
        return version switch
        {
            LanguageVersion.CSharp7_3 => "7.3",
            LanguageVersion.CSharp7_2 => "7.2",
            LanguageVersion.CSharp7_1 => "7.1",
            LanguageVersion.CSharp7 => "7.0",
            LanguageVersion.CSharp6 => "6.0",
            LanguageVersion.CSharp9_0 => "9.0",
            LanguageVersion.CSharp8_0 => "8.0",
            LanguageVersion.CSharp10_0 => "10.0",
            LanguageVersion.CSharp11_0 => "11.0",
            LanguageVersion.CSharp1 => "1.0",
            LanguageVersion.CSharp2 => "2.0",
            LanguageVersion.CSharp3 => "3.0",
            LanguageVersion.CSharp4 => "4.0",
            LanguageVersion.CSharp5 => "5.0",
            LanguageVersion.CSharp12_0 => "12.0",
            LanguageVersion.CSharp13_0 => "13.0",
            LanguageVersion.Latest => "latest",
            _ =>
                // 针对老版本或者未定义，简单返回数字部分
                version.ToString().Replace("CSharp", "").Replace("_", ".").ToLower(),
        };
    }
}


