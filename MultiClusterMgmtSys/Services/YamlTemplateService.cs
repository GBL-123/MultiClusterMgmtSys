using Microsoft.AspNetCore.Hosting;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// YAML 模板提供者:为创建对话框按类别与名称返回初始 YAML 文本。
/// </summary>
public interface IYamlTemplateService
{
    /// <summary>读取指定类别的 YAML 模板;文件缺失或读取失败时返回最小骨架回退文本,不抛异常。</summary>
    /// <param name="category">资源类别(模板目录名),如 workload/configmap/service。</param>
    /// <param name="name">模板名(文件名,不含扩展名),同时用于生成回退骨架的 kind。</param>
    /// <returns>模板 YAML 文本。</returns>
    Task<string> GetTemplateAsync(string category, string name);
}

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
