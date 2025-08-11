using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;

namespace CSharpPatchDroid
{
    public static class WriteCsproj
    {
        public static void Start(
            string filePath,
            string assemblyName,
            string targetFramework,
            string langVersion,
            bool allowUnsafe,
            List<(string Include, string HintPath)> references)
        {
            Print.print("正在生成.csproj文件中");

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 主 Project 元素
            var project = new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),

                // PropertyGroup
                new XElement("PropertyGroup",
                    new XElement("AssemblyName", assemblyName),
                    new XElement("GenerateAssemblyInfo", "False"),
                    new XElement("TargetFramework", targetFramework),
                    new XElement("OutputType", "Library"),
                    new XElement("LangVersion", langVersion),
                    new XElement("AllowUnsafeBlocks", allowUnsafe ? "True" : "False")
                ),

                // 默认编译规则
                new XElement("ItemGroup",
                    new XElement("Compile", new XAttribute("Include", "**/*.cs"))
                )
            );

            // 添加引用
            if (references != null && references.Count > 0)
            {
                var refGroup = new XElement("ItemGroup");
                foreach (var (include, hintPath) in references)
                {
                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(filePath), hintPath);
                    refGroup.Add(
                        new XElement("Reference",
                            new XAttribute("Include", include),
                            new XElement("HintPath", relativePath)
                        )
                    );
                }
                project.Add(refGroup);
            }

            // 生成 XML
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), project);
            doc.Save(filePath);

            Print.sucess($"生成.csproj文件成功：{filePath}");
        }
    }
}