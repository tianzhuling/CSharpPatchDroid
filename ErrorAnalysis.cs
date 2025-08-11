using System;
using System.Linq;
using System.Collections.Generic;
using CSharpPatchDroid;
using Microsoft.CodeAnalysis;
namespace CSharpPatchDroid;

public class ErrorAnalysis
{
    public int line;
    public Diagnostic diag;
    public int character;
    public string type;
    public string message;
    public string csPath;
    public string referenceInfo;
    public Reference reference;
    public ErrorAnalysis(Diagnostic diag)
    {
        this.diag = diag;
        var span = diag.Location.GetLineSpan();
        this.csPath = span.Path;
        this.line = span.StartLinePosition.Line + 1;
        this.character = span.StartLinePosition.Character + 1;
        this.type = diag.Id;

        this.message = diag.GetMessage();

    }
    public Reference getReference()
    {
        if (this.type != "CS0012")
        {
            Print.error("只能获取类型为CS0012的依赖");
            Program.log.error("只能获取类型为CS0012的依赖");
            return null;
        }
        //Print.print(this.diag.ToString());
        this.referenceInfo = this.diag.ToString().Split("”在未引用的程序集中定义。必须添加对程序集“")[1].Split("”的引用。")[0];
        var include = referenceInfo.Split(",")[0];
        var version = referenceInfo.Split(", Version=")[1].Split(",")[0];
        var culture = referenceInfo.Split(", Culture=")[1].Split(",")[0];
        var publicKeyToken = referenceInfo.Split(", PublicKeyToken=")[1].Split("”的引用。")[0];
        return new Reference(include, version, culture, publicKeyToken);

    }
    public class Reference
    {
        public string include;
        public string version;
        public string culture;
        public string publicKeyToken;
        public string hintPath;
        public Reference(string include, string version, string culture, string publicKeyToken)
        {
            this.include = include;
            this.version = version;
            this.culture = culture;
            this.publicKeyToken = publicKeyToken;
        }
        public void setHintPath(string hintPath)
        {
            this.hintPath = hintPath;
        }


    }

}
