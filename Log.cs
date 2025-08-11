using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
namespace CSharpPatchDroid;

public class Log
{
    public string logPath;
    public Log(string path)
    {
        this.logPath = path;
    }
    public string log(string content, int level, string place = "None")
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string data = "[" + time + "]" + "[" + place + "]" + "[" + level.ToString() + "]" + content + Environment.NewLine;
        File.AppendAllText(logPath, data);
        return data;
    }
    public string error(string content, string place = "None")
    {
        return log("错误:" + content, 3, place);

    }
    public string warning(string content, string place = "None")
    {
        return log("警告:" + content, 2, place);
    }
    public string ok(string content, string place = "None")
    {
        return log("成功:" + content, 0, place);

    }
    public string tip(string content, string place = "None")
    {
        return log("提示:" + content, 1, place);
    }
}
