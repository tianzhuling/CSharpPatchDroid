using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO.Compression;
namespace CSharpPatchDroid;
class NugetDownloader
{
    private static readonly string NugetSource = "https://api.nuget.org/v3/index.json";
    public static int number;
    /// <summary>
    /// 根据关键词搜索 NuGet 包，返回最相关的包 ID
    /// </summary>
    public static async Task<string?> SearchPackageAsync(string keyword)
    {
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(new PackageSource(NugetSource), providers);

        var searchResource = await repository.GetResourceAsync<PackageSearchResource>();
        var results = await searchResource.SearchAsync(
            keyword,
            new SearchFilter(includePrerelease: true),
            skip: 0,
            take: 5, // 取前5个最相关的
            log: NullLogger.Instance,
            cancellationToken: CancellationToken.None);

        var bestMatch = results.FirstOrDefault();
        if (bestMatch == null)
            return null;

        //Console.WriteLine($"🔍 搜索 {keyword} → 选择 {bestMatch.Identity.Id}");
        return bestMatch.Identity.Id;
    }

    /// <summary>
    /// 下载 NuGet 包中的 DLL
    /// </summary>
    public static async Task DownloadDllAsync(string packageId, string? requestedVersion, string outputDir)
    {
        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(new PackageSource(NugetSource), providers);

        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();

        // 获取所有版本
        var versions = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            cache,
            logger,
            CancellationToken.None);

        if (!versions.Any())
        {
            Console.WriteLine($"❌ 没找到包 {packageId}");
            return;
        }

        NuGetVersion versionToUse;

        if (!string.IsNullOrEmpty(requestedVersion) &&
            NuGetVersion.TryParse(requestedVersion, out var requested))
        {
            // 如果该版本存在
            if (versions.Any(v => v.Identity.Version == requested))
            {
                versionToUse = requested;
            }
            else
            {
                // 否则取最新版本
                versionToUse = versions.Max(v => v.Identity.Version);
                //Console.WriteLine($"⚠️ 指定版本 {requestedVersion} 不存在，已使用最新版本 {versionToUse}");
            }
        }
        else
        {
            // 如果没指定版本，就直接取最新
            versionToUse = versions.Max(v => v.Identity.Version);
            //Console.WriteLine($"ℹ️ 未指定版本，已使用最新版本 {versionToUse}");
        }

        var findPackage = await repository.GetResourceAsync<FindPackageByIdResource>();
        Directory.CreateDirectory(outputDir);

        using (var packageStream = new MemoryStream())
        {
            bool success = await findPackage.CopyNupkgToStreamAsync(
                packageId,
                versionToUse,
                packageStream,
                cache,
                logger,
                CancellationToken.None);

            if (!success)
            {
                Print.warning($"❌ 下载失败 {packageId} {versionToUse}");
                return;
            }

            packageStream.Position = 0;

            // 解压 nupkg (zip 格式)
            using (var archive = new System.IO.Compression.ZipArchive(packageStream, System.IO.Compression.ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                {
                    var filePath = Path.Combine(outputDir, Path.GetFileName(entry.FullName));
                    entry.ExtractToFile(filePath, true);
                    //Console.WriteLine($"✅ 已下载 {filePath}");

                }
            }
        }
        Print.sucess($"{packageId}下载完成！");
        number++;
    }
    public static async Task Start(string keyword, string outputDir, string version)
    {
        // 1️⃣ 定义你要搜索的关键词（可以是包名）
        //string keyword = "Newtonsoft.Json";  
        // 2️⃣ 搜索最相关的包

        string? packageId = await NugetDownloader.SearchPackageAsync(keyword);
        if (packageId == null)
        {
            Print.warning($"未找到{keyword}的 NuGet 包");
            return;
        }

        // 3️⃣ 定义输出目录
        //string outputDir = @"C:\Temp\NugetDlls";  // Windows 示例
        // string outputDir = "/sdcard/Download/NugetDlls"; // Android/Termux 示例

        // 4️⃣ 指定版本（可选），如果不想指定版本可以传 null
        //string? version = null; // 例如 "13.0.1"

        // 5️⃣ 下载 DLL
        await NugetDownloader.DownloadDllAsync(packageId, version, outputDir);



    }
}