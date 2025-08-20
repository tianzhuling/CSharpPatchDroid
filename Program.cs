using System;
using System.Linq;
using System.IO;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using System.Collections.Generic;
using System.Net.Http;
using System.IO.Compression;
using Android.App;
using Android.Content.Res;
using Microsoft.Build.Locator;
using System.Reflection;
using System.Runtime.Versioning;
using dnlib.DotNet;
using F23.StringSimilarity;
//using NativeUiLib;
using CSharpPatchDroid;
namespace CSharpPatchDroid;
public static class Program
{
    public static string HOME { get; }
    public static ReadSettings.Settings settings { get; }
    public static List<string> projects;//弃用
    public static Dictionary<string, List<string>> commandTips { get; }
    public static Log log { get; }
    public static string projectPathForRelativePathMode;
    public static string dllPathForRelativePathMode;
    //public static string DOTNET;
    static Program()

    {
        try
        {

            Directory.CreateDirectory("/storage/emulated/0/CSharpPatchDroidOutput/");
        }
        catch
        {
            Print.error("请给手机赋予存储权限");
            Console.Write("回车以继续:");
            Console.ReadLine();
        }
        log = new Log("/storage/emulated/0/CSharpPatchDroidOutput/log.txt");

        if (!Path.Exists("/storage/emulated/0/CSharpPatchDroidOutput/settings.json"))
        {
            log.tip("检测到未创建/storage/emulated/0/CSharpPatchDroidOutput/settings.json");
            ReadSettings.InitSettings();
        }
        settings = ReadSettings.Read();
        HOME = settings.HOME;

        if (!Path.Exists(HOME))
        {
            log.warning($"HOME路径不存在:{HOME}");
            Directory.CreateDirectory(HOME);
        }
        commandTips = new Dictionary<string, List<string>>();
        commandTips.Add("create", new List<string>() { "#必填 dll路径", "#选填 C#版本", "#选填 框架(.NETCOREAPP,.NETSTANDARD,.NETFRAMEWORK,SILVERLIGHT，默认.NETFRAMEWORK)", "#选填 框架版本", "#选填 项目名称", "#选填 项目根目录(默认为HOME)" });
        commandTips.Add("sethome", new List<string>() { "#必填 新HOME路径" });
        commandTips.Add("homepath", new List<string>());
        commandTips.Add("updateproject", new List<string>() { "#必填 项目名称", "#必填 根目录路径(默认为HOME目录)" });
        commandTips.Add("update", new List<string>() { "#变化 项目名称", "#变化 根目录路径(默认为HOME目录)", "#选填 .csproj文件路径(默认自动查找)" });
        commandTips.Add("clear", new List<string>() { "确定?(确定输入Y，以后都是Y/n)", "你将要删除/storage/emulated/0/CSharpPatchDroidOutput中的所有内容", "最后一道防线" });
        commandTips.Add("resetsettings", new List<string>() { });
        commandTips.Add("restart", new List<string>() { });
        commandTips.Add("exit", new List<string>() { });

        //commandTips.Add("editsettings", new List<string>() { "准备修改的设置项", "修改后的值" });
        //DOTNET = "/storage/emulated/0/CSharpPatchDroidOutput/dotnet/sdk/8.0.412";
        log.ok("完成初始化");

    }
    public static void Main()
    {
        //init();
        //LoadProjects();
        while (true)
        {
            Console.Write("[命令]>>>");
            string command = Console.ReadLine();
            string thisArg;
            if (!commandTips.Keys.Contains(command))
            {
                Print.error($"不存在的命令: \"{command}\"");
                log.error($"命令错误:{command}", "Program,Main Line70");
                foreach (var i in commandTips.Keys)
                {
                    var jw = new JaroWinkler();
                    double similarity = jw.Similarity(i, command);
                    if (similarity > 0.7)
                    {
                        Print.print($"你是在说\"{i}\"吗");
                        continue;
                    }

                }
                continue;

            }
            List<string> args = new List<string>();
            thisArg = "";
            int count = 0;
            bool quit = false;
            List<string> tips;
            tips = commandTips[command];
            if (settings.absolutePathMode)
            {
                if (command == "create")
                {
                    tips = new List<string>() { "#必填 dll路径", "#选填 C#版本", "#选填 框架(.NETCOREAPP,.NETSTANDARD,.NETFRAMEWORK,SILVERLIGHT，默认.NETFRAMEWORK)", "#选填 框架版本", "#选填 输出目录" };
                    command = "createAbsolute";
                }
                else if (command == "update")
                {
                    tips = new List<string>() { "#变化 项目路径", "#选填 .csproj文件路径(默认自动查找)" };
                    command = "updateAbsolute";
                }
            }
            while (count < tips.Count)
            {
                string tip = tips[count];
                count++;
                Console.Write("[参数 " + count + " ](" + tip + ")>>>");
                thisArg = Console.ReadLine();
                if (thisArg == "/over")
                {
                    break;
                }

                if (thisArg == "/quit")
                {
                    quit = true;
                    break;
                }
                if (thisArg != "" && thisArg != "/skip")
                {
                    args.Add(thisArg);
                }
                else
                {
                    args.Add(null);
                }
            }
            if (quit)
            {
                continue;
            }
            Print.print(new String('-', 45));
            while (args.Count < tips.Count)
            {
                args.Add(null);
            }

            if (command == "create")
            {
                CreateProject(args);

            }
            if (command == "updateproject")
            {
                UpdateProject(args);

            }
            if (command == "update")
            {
                var success = Update(args);
                if (success)
                {
                    Print.sucess("编译成功");
                }
                else
                {
                    Print.error("编译失败");
                    log.error("编译异常", "Program,Main Line132");
                }
            }
            if (command == "sethome")
            {
                Print.warning("此方法不再可用");
                log.warning("尝试访问了不可用的方法:SetHome");
                //SetHome(args);
            }
            if (command == "homepath")
            {
                Print.print(HOME);
            }
            if (command == "clear")
            {
                clear(args);

            }
            if (command == "resetsettings")
            {
                ReadSettings.InitSettings();
                Print.sucess("重置设置成功");
            }
            if (command == "restart")
            {
                Console.Clear();
                Main();
            }
            if (command == "exit")
            {
                return;
            }
            if (command == "createAbsolute")
            {
                createAbsolute(args);
            }
            if (command == "updateAbsolute")
            {
                updateAbsolute(args);
            }
            Print.print(new String('-', 45));

        }

    }
    public static void clear(List<string> args)
    {
        if (args[0] == "Y" && args[1] == "Y" && args[2] == "Y")
        {
            Directory.Delete("/storage/emulated/0/CSharpPatchDroidOutput", recursive: true);
            Print.sucess("删除成功");
            return;
        }
    }
    public static void LoadProjects()
    {
        Print.warning("此方法已弃用");
        log.warning("使用了弃用的方法LoadProjects");
        List<string> dirs = Directory.GetDirectories(HOME).ToList();
        projects = dirs;

    }
    /*
    public static void SetHome(List<string> args)
    {
        Print.warning("此方法已弃用，原因是防止乱改HOME路径");
        HOME = args[0];
    }
    */
    public static bool updateAbsolute(List<string> args)
    {
        if (args[0] == null)
        {
            Print.error("绝对路径模式下，项目路径是必填的");
            return false;
        }
        else if (!Path.Exists(args[0]))
        {
            Print.error("项目路径不存在");
            return false;
        }
        return Update(new List<string>() { Path.GetFileName(args[0]), Path.GetDirectoryName(args[0]), args[1] });
    }
    public static void createAbsolute(List<string> args)
    {
        /*
        Print.print(Path.GetFileName(args[4]));
        Print.print(Path.GetDirectoryName(args[4]));
        */

        string outputName;
        string outputDir;
        if (args[0] == null)
        {
            Print.error("dll路径是必填的");
            log.error("dll路径为空");
            return;
        }
        if (!Path.Exists(args[0]))
        {
            Print.error("dll路径不存在");
            log.error($"dll路径不存在:{args[0]}", "Program,createAbsolute Line263");
            return;
        }
        if (args[4] == null)
        {
            outputName = Path.GetFileNameWithoutExtension(args[0]);
            outputDir = Path.GetDirectoryName(args[0]);
        }
        else
        {
            outputName = Path.GetFileName(args[4]);
            outputDir = Path.GetDirectoryName(args[4]);
        }
        CreateProject(new List<string>() { args[0], args[1], args[2], args[3], outputName, outputDir });
    }
    public static bool UpdateProject(List<string> args)
    {
        var projectName = args[0];
        var projectRoot = args[1];
        if (args[1] == null)
        {
            log.tip("默认将根目录设为HOME");
            projectRoot = HOME;
        }
        string projectPath = Path.Join(projectRoot, projectName);
        string csprojPath;
        List<string> projectFiles = Directory.GetFiles(projectPath).ToList();
        projectFiles.RemoveAll(projectFile => !projectFile.EndsWith(".csproj"));

        if (projectFiles.Count == 1)
        {
            csprojPath = projectFiles[0];
        }
        else if (projectFiles.Count > 1 && projectFiles.Contains(Path.Join(projectPath, projectName + ".csproj")))
        {
            csprojPath = Path.Join(projectPath, projectName + ".csproj");

        }
        else
        {
            Print.error("没有找到.csproj文件");
            return false;
        }
        //string csprojPath = Path.Join(projectPath, projectName + ".csproj");
        Directory.CreateDirectory(Path.Combine("/storage/emulated/0/CSharpPatchDroidOutput/bin", projectName));
        string outputDir;
        outputDir = Path.Combine("/storage/emulated/0/CSharpPatchDroidOutput/bin", projectName, projectName + ".dll");
        return Compiler.Start(csprojPath, outputDir, projectPath);

    }
    public static bool Update(List<string> args)
    {
        string projectPath;
        string projectName;
        string projectRoot;
        if (settings.relativePathMode)
        {
            if (projectPathForRelativePathMode != null)
            {
                projectPath = projectPathForRelativePathMode;
                projectName = Path.GetFileName(projectPathForRelativePathMode);
                projectRoot = Directory.GetDirectoryRoot(projectPathForRelativePathMode);
            }
            else
            {
                projectName = args[0];

                if (args[1] == null)
                {


                    projectRoot = HOME;


                }
                else
                {
                    projectRoot = args[1];
                }
                projectPath = Path.Join(projectRoot, projectName);

            }
        }
        else
        {
            projectName = args[0];

            if (args[1] == null)
            {


                projectRoot = HOME;


            }
            else
            {
                projectRoot = args[1];
            }
            projectPath = Path.Join(projectRoot, projectName);
        }
        string csprojPath;
        List<string> projectFiles = Directory.GetFiles(projectPath).ToList();
        if (args[2] == null)
        {
            projectFiles.RemoveAll(projectFile => !projectFile.EndsWith(".csproj"));

            if (projectFiles.Count == 1)
            {
                csprojPath = projectFiles[0];
            }
            else if (projectFiles.Count > 1 && projectFiles.Contains(Path.Join(projectPath, projectName + ".csproj")))
            {
                csprojPath = Path.Join(projectPath, projectName + ".csproj");

            }
            else
            {
                Print.error("没有找到.csproj文件");
                return false;
            }
        }
        else
        {
            if (!Path.Exists(args[2]))
            {
                Print.error(".csproj文件路径错误:路径不存在在");
                log.error("输入了一个错误的.csproj路径", "Program,Update Line182");
                return false;
            }
            else
            {
                csprojPath = args[2];
            }
        }
        Directory.CreateDirectory(Path.Combine("/storage/emulated/0/CSharpPatchDroidOutput/bin", Path.GetFileNameWithoutExtension(projectName)));
        string outputDir;
        if (settings.relativePathMode)
        {
            if (dllPathForRelativePathMode != null)
            {
                outputDir = dllPathForRelativePathMode;
            }
            else
            {
                outputDir = Path.Combine("/storage/emulated/0/CSharpPatchDroidOutput/bin", Path.GetFileNameWithoutExtension(projectName), Path.GetFileNameWithoutExtension(projectName) + ".dll");
            }
        }
        else
        {
            outputDir = Path.Combine("/storage/emulated/0/CSharpPatchDroidOutput/bin", Path.GetFileNameWithoutExtension(projectName), Path.GetFileNameWithoutExtension(projectName) + ".dll");

        }
        Print.print("开始编译");
        Print.print("编译中");
        return Compiler.Start(csprojPath, outputDir, projectPath);
    }

    public static void CopySdk()
    {
        log.warning("使用了弃用的方法:CopySdk");
        AssetManager assets = Application.Context.Assets;
        using (Stream assetStream = assets.Open("dotnetSdk.zip"))
        {

            using (var outStream = File.Create("/storage/emulated/0/CSharpPatchDroidOutput/dotnetSdk/dotnetSdk.zip"))
            {
                assetStream.CopyTo(outStream);
            }
        }

    }
    public static Decompiler CreateProject(List<string> args)
    {
        log.tip("已开始反编译项目");

        if (args[0] == null)
        {
            Print.error("dll路径是必填的");
            log.error("dll路径为空");
            return null;
        }
        if (!Path.Exists(args[0]))
        {
            Print.error("dll路径不存在");
            log.error($"dll路径不存在:{args[0]}", "Program,Main Line194");
            return null;
        }
        var rootDir = args[5];
        if (rootDir == null)
        {
            if (settings.relativePathMode)
            {
                rootDir = Path.GetDirectoryName(args[0]);
            }
            else
            {
                rootDir = HOME;
            }
        }
        settings.referenceRoot = Path.GetDirectoryName(args[0]);
        string targetFramework;
        if (args[2] == null && args[3] == null)
        {
            targetFramework = GetVersion(args[0]);

        }
        else
        {
            string fw = args[2];
            if (fw == null)
            {
                fw = ".NETFRAMEWORK";
            }
            string fwv = args[3];
            if (args[3] == null)
            {
                Print.error("需指定.NET版本");
                return null;
            }
            targetFramework = fw + ",Version=v" + fwv;

        }
        log.tip("targetFramework");
        //Print.print(targetFramework);
        LanguageVersion lv;
        if (args[1] == null)
        {
            var inputlv = CSharpVersionMapper.GetMaxCSharpVersion(targetFramework);
            var normallv = GetLangVersionNormal(inputlv);
            if (Enum.TryParse(normallv, ignoreCase: true, out LanguageVersion languagev))
            {
                lv = languagev;

            }
            else
            {
                lv = LanguageVersion.Latest;
            }

        }
        else
        {
            string inputCSharpv;
            if (!args[1].Contains("CSharp"))
            {
                inputCSharpv = GetLangVersionNormal(args[1]);
                if (inputCSharpv == "error")
                {
                    Print.error("发生错误，程序已终止");
                    log.error("反编译异常", "Program,CreateProject Line182");
                    return null;
                }

            }
            else
            {
                inputCSharpv = args[1];
            }
            if (Enum.TryParse(inputCSharpv, ignoreCase: true, out LanguageVersion languagev))
            {
                lv = languagev;

            }
            else
            {
                lv = LanguageVersion.Latest;
            }
        }
        log.tip("C#版本:" + lv.ToString());

        string outputDir;
        string fileName;
        string name = args[4];
        if (name != null)
        {
            fileName = name;
            outputDir = Path.Join(rootDir, fileName);


        }
        else
        {
            fileName = Path.GetFileNameWithoutExtension(args[0]);
            outputDir = Path.Join(rootDir, fileName);

        }
        int count = 1;
        while (Path.Exists(outputDir))
        {
            count++;
            outputDir = Path.Join(rootDir, fileName + '(' + count + ')');
        }
        Decompiler decompiler = new Decompiler(args[0], lv, outputDir, targetFramework);
        if (settings.relativePathMode)
        {
            projectPathForRelativePathMode = outputDir;
            dllPathForRelativePathMode = args[0];
        }
        /*
        int i = 1;
        var lay = new LinearLayout();
        var btn = lay.AddButton();
        btn.Text = "Hello, Native UI";

        btn.Click += delegate
        {
            btn.Text = i.ToString();
            i++;
        };

        lay.Show();
        */
        /*
        if (args[5] != null)
        {
            HOME = "/storage/emulated/0/CSharpPatchDroidOutput/projects";

        }
        */
        return decompiler;

    }
    public static string GetLangVersionNormal(string version)
    {
        string end = version switch
        {
            "7.3" => "CSharp7_3",
            "7.2" => "CSharp7_2",
            "7.0" => "CSharp7",
            "6.0" => "CSharp6",
            "9.0" => "CSharp9",
            "8.0" => "CSharp8",
            "11.0" => "CSharp11",
            "10.0" => "CSharp10",
            "1.0" => "CSharp1",
            "2.0" => "CSharp2",
            "3.0" => "CSharp3",
            "4.0" => "CSharp4",
            "5.0" => "CSharp5",
            "12.0" => "CSharp12",
            "13.0" => "CSharp13",
            "latest" => "Latest",
            "preview" => "Preview",
            "default" => "Default",
            _ => "error"
        };
        if (end == "error")
        {
            Print.error("C#版本错误");
            log.error("C#版本错误", "Program,GetLangVersionNormal Line280");
            return "error";
        }
        else
        {
            return end;
        }
    }
    public static string GetVersion(string dllPath)
    {
        try
        {
            var module = ModuleDefMD.Load(dllPath);

            // 尝试获取 TargetFrameworkAttribute
            foreach (var attr in module.Assembly.CustomAttributes)
            {
                if (attr.TypeFullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    return attr.ConstructorArguments[0].Value.ToString();
                }
            }

            // 如果未找到 TargetFrameworkAttribute，检查是否引用了 System.Core
            bool refsSystemCore = module.GetAssemblyRefs()
                                        .Any(asmRef => asmRef.Name.Contains("System.Core"));

            // 根据引用情况推测目标框架版本
            if (refsSystemCore)
                return ".NETFramework,Version=v3.5";
            else
                return ".NETFramework,Version=v3.0";
        }
        catch (Exception ex)
        {
            // 记录异常信息
            Console.WriteLine($"Error processing {dllPath}: {ex.Message}");
            return null;
        }
    }

}
