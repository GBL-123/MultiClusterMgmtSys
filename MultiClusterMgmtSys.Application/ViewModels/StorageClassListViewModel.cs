namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// StorageClass 存储类列表页展示数据(只读浏览;SHALL NOT 提供详情页与写操作)。
/// </summary>
public class StorageClassListViewModel
{
    /// <summary>存储类名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>制备器(Driver)。</summary>
    public string Provisioner { get; set; } = "";

    /// <summary>回收策略(Retain/Delete)。</summary>
    public string ReclaimPolicy { get; set; } = "";

    /// <summary>卷绑定模式(Immediate/WaitForFirstConsumer)。</summary>
    public string VolumeBindingMode { get; set; } = "";

    /// <summary>是否允许卷扩展;null 表示 API 未设置。</summary>
    public bool? AllowVolumeExpansion { get; set; } = null;

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
