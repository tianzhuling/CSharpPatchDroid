using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
namespace CSharpPatchDroid;
//弃用Unzip
public static class Unzip
{
    public static void Start(string targetFile, string outputDir)
    {
        Print.print("正在解压.NET sdk中……");
        // 1. 打开 zip 文件流

        using (var stream = File.OpenRead(targetFile))
        // 2. 用 Reader 解压 .tar.gz（SharpCompress 支持自动解gzip+tar）
        using (var archive = ArchiveFactory.Open(stream))
        {
            foreach (var entry in archive.Entries)
            {
                // 检查条目的 Key 是否为 null 或空
                if (string.IsNullOrEmpty(entry.Key))
                {
                    Print.warning("此条目Key为空，已跳过。");
                    continue; // 跳过该条目
                }

                if (!entry.IsDirectory)
                {
                    // 解压到目标目录
                    entry.WriteToDirectory(outputDir, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }
        Print.sucess("解压成功");
    }
}