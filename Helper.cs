using System;
using System.Linq;
using System.Collections.Generic;

namespace CSharpPatchDroid;

public static class Helper
{
    /*
    commandTips.Add("create", new List<string>() { "dll路径", "C#版本", "框架(.NETCOREAPP,.NETSTANDARD,.NETFRAMEWORK,SILVERLIGHT)默认.NETFRAMEWORK", "框架版本", "项目名称", "输出根路径(默认在/sdcard/CSharpPatchDroidOutput/projects)" });
    commandTips.Add("sethome", new List<string>() { "新HOME路径" });
    commandTips.Add("homepath", new List<string>());
    commandTips.Add("updateproject", new List<string>() { "项目名称", "根目录路径(HOME目录，默认/storage/emulated/0/CSharpPatchDroidOutput/projects)" });
    commandTips.Add("update", new List<string>() { "项目名称", "根目录路径(HOME目录，默认/storage/emulated/0/CSharpPatchDroidOutput/projects)", ".csproj文件路径(默认自动查找)" });
    commandTips.Add("clear", new List<string>() { "确定?(确定输入Y，以后都是Y/n)", "你将要删除/storage/emulated/0/CSharpPatchDroidOutput中的所有内容", "最后一道防线" });
    */
    public static void needHelp(string command, int arg)
    {
        if (command == "create")
        {
            Create(arg);
        }
        else if (command == "update")
        {
            Update(arg);
        }
    }
    public static void Create(int arg)
    {
        if (arg == 1)
        {
            Print.print("动态链接库，是C#的二进制产物，存放代码逻辑。安卓UnityMono中，它的路径是 /assets/bin/Data/Managed/Assembly-CSharp.dll");
        }
        else if (arg == 2)
        {
            Print.print("C#的版本，请输入 x.x 格式的C#版本，指定C#版本并不会影响反编译和编译结果，但请将C#版本设置为.NET版本支持的，如果不填则自动补全");

        }
        else if (arg == 3)
        {
            Print.print(".NET框架，请输入 .NETCOREAPP,.NETSTANDARD,.NETFRAMEWORK,SILVERLIGHT 这四种框架中的一个，大小写不敏感，默认是.NETFRAMEWORK，在旧版UnityMono游戏中，这是较为常见的");

        }
        else if (arg == 4)
        {
            Print.print(".NET版本，请输入 x.x这样的格式，如果不填则自动补全");

        }
        else if (arg == 5)
        {
            Print.print("项目的名称，默认是dll的文件名不带后缀");
        }
        else if (arg == 6)
        {
            Print.print("反编译结果的根目录，默认是HOME");
        }
    }
    public static void Update(int arg)
    {
        if (arg == 1)
        {
            Print.print("项目的名称，在非relativePathMode下这是必填的，就相当于反编译出来的文件夹名称");

        }
        else if (arg == 2)
        {
            Print.print("反编译结果的根目录，默认是HOME，相当于反编译结果的父目录完整路径,非relativePathMode下是必填的");
        }
        else if (arg == 3)
        {
            Print.print("默认是反编译结果目录下的.csproj文件，里面涵盖了该反编译结果的信息，非必填，留空自动查找");
        }
    }
}
