using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Application.Common.Secrets;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// V1 Secret → 密钥展示模型的映射(列表/详情/编辑)。
/// </summary>
public static class SecretMappingExtensions
{
    /// <summary>将 <see cref="V1Secret"/> 映射为 Secret 列表展示数据。</summary>
    public static SecretListViewModel ToSecretListViewModel(this V1Secret secret)
    {
        var keyCount = (secret.Data?.Count ?? 0) + (secret.StringData?.Count ?? 0);
        return new SecretListViewModel
        {
            Name = secret.Metadata?.Name ?? "",
            Namespace = secret.Metadata?.NamespaceProperty ?? "",
            Type = secret.Type ?? "",
            KeyCount = keyCount,
            CreatedAt = secret.Metadata?.CreationTimestamp
        };
    }

    /// <summary>将 <see cref="V1Secret"/> 映射为 Secret 详情展示数据;includeContent=false 时(受限态)不投影键与 YAML。</summary>
    public static SecretDetailViewModel ToSecretDetailViewModel(this V1Secret secret, bool includeContent)
    {
        var vm = new SecretDetailViewModel
        {
            Name = secret.Metadata?.Name ?? "",
            Namespace = secret.Metadata?.NamespaceProperty ?? "",
            Uid = secret.Metadata?.Uid ?? "",
            CreatedAt = secret.Metadata?.CreationTimestamp,
            Type = secret.Type ?? ""
        };

        if (!includeContent)
        {
            return vm;
        }

        vm.Yaml = KubernetesYaml.Serialize(secret);
        foreach (var kv in secret.Data ?? new Dictionary<string, byte[]>())
        {
            var bytes = kv.Value ?? [];
            var isText = SecretRedaction.IsUtf8Text(bytes);
            vm.Entries.Add(new SecretKeyItemViewModel
            {
                Key = kv.Key,
                ByteCount = bytes.Length,
                IsText = isText,
                Base64 = isText ? "" : Convert.ToBase64String(bytes)
            });
        }

        return vm;
    }

    /// <summary>将 <see cref="V1Secret"/> 映射为编辑页展示数据(YAML 以占位符回显,契约见 secrets-page)。</summary>
    public static SecretEditViewModel ToSecretEditViewModel(this V1Secret secret)
    {
        return new SecretEditViewModel
        {
            Name = secret.Metadata?.Name ?? "",
            Namespace = secret.Metadata?.NamespaceProperty ?? "",
            Type = secret.Type ?? "",
            Yaml = KubernetesYaml.Serialize(SecretRedaction.ApplyPlaceholders(secret))
        };
    }
}
