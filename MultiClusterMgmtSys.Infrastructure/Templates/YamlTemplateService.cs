using Microsoft.AspNetCore.Hosting;
using MultiClusterMgmtSys.Application.Abstractions;

namespace MultiClusterMgmtSys.Infrastructure.Templates;

/// <summary>
/// 从 wwwroot/templates/{category}/{name}.yaml 读取创建对话框的 YAML 模板。
/// 文件缺失或读取失败时回退为最小骨架并记录警告,不阻塞对话框打开。
/// Docker 部署可通过 volume 挂载 wwwroot/templates 覆盖模板。
/// </summary>
public class YamlTemplateService(IWebHostEnvironment env, ILogger<YamlTemplateService> logger) : IYamlTemplateService
{
    /// <summary>按类别/名称读取 wwwroot/templates 下的模板文件,缺失或读取失败时回退骨架并记录警告。</summary>
    public async Task<string> GetTemplateAsync(string category, string name)
    {
        try
        {
            var webRoot = env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            }
            var path = Path.Combine(webRoot, "templates", category, $"{name}.yaml");
            if (!File.Exists(path))
            {
                logger.LogWarning("YAML template missing: templates/{Category}/{Name}.yaml", category, name);
                return Fallback(category, name);
            }
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "YAML template load failed: templates/{Category}/{Name}.yaml", category, name);
            return Fallback(category, name);
        }
    }

    private static string Fallback(string category, string name)
    {
        var kind = name.Length == 0 ? "" : char.ToUpperInvariant(name[0]) + name[1..];
        return $"""
            # 模板文件缺失: templates/{category}/{name}.yaml
            # 请手工补全以下骨架
            apiVersion: v1
            kind: {kind}
            metadata:
              name: 
              namespace: 
            """;
    }
}
