using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using CSharpPatchDroid;
namespace CSharpPatchDroid;

public static class ReadSettings
{
    public static string PATH;
    public static void init()
    {
        Program.log.tip("ReadSettings被调用init");
        PATH = "/storage/emulated/0/CSharpPatchDroidOutput/settings.json";
    }
    public static void InitSettings()
    {
        init();
        var data = new
        {
            HOME = "/storage/emulated/0/CSharpPatchDroidOutput/projects",
            relativePathMode = false,
            autoComplete = true,
            autoCompleteReference = true,
            referenceRoot = "../",
            absolutePathMode = true
        };
        string Settings = JsonSerializer.Serialize(data);
        File.WriteAllText(PATH, Settings);
    }
    public static Settings Read()
    {
        init();
        Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(PATH));
        return settings;
    }
    public class Settings()
    {
        public bool relativePathMode { get; set; }
        public bool autoComplete { get; set; }
        public bool autoCompleteReference { get; set; }
        public string HOME { get; set; }
        public string referenceRoot { get; set; }
        public bool absolutePathMode { get; set; }
    }

}
