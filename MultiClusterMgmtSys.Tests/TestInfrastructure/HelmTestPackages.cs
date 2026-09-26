using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>内存构造 chart 包(.tgz)供解析与安装相关测试使用。</summary>
public static class HelmTestPackages
{
    /// <summary>以给定条目构造一个 gzip tar 包。</summary>
    /// <param name="files">条目名与文本内容。</param>
    /// <returns>.tgz 字节内容。</returns>
    public static byte[] Create(params (string Name, string Content)[] files)
    {
        using var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CompressionLevel.SmallestSize, leaveOpen: true))
        using (var tar = new TarWriter(gzip, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
                };
                tar.WriteEntry(entry);
            }
        }
        return stream.ToArray();
    }
}
