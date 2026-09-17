namespace MultiClusterMgmtSys.Application.Abstractions;

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
