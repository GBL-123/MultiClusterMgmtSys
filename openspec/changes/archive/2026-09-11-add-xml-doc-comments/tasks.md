## 1. 基础开关与小目录查缺补漏

- [x] 1.1 主项目 csproj 开启 `GenerateDocumentationFile`(已临时开启,确认保留)
- [x] 1.2 Common/Enums 7 个枚举:类型与每个成员补中文注释(WorkloadKind/AuditAction/AuditCategory/ClusterSortField/ClusterStatus/ConnectionType/ClusterEndpointKind)
- [x] 1.3 Common/Exceptions 7 个异常类 + K8sExceptionMapper:查缺补漏中文注释
- [x] 1.4 Models/ 3 个文件(ClusterPageQuery/VersionFilterSentinel 等)补齐并将英文契约注释翻成中文
- [x] 1.5 Components/Common 4 个 .cs(ExceptionPresenter/ThemeManager 等)补中文注释
- [x] 1.6 .razor 3 处英文注释(ClusterTable/ConfigMaps/Nodes)翻成中文
- [x] 1.7 构建:0 错误,剩余 CS1591 仅来自 Services/Data/Requests/ViewModels

## 2. Services 全量注释

- [x] 2.1 ClusterService(11 方法)、ClusterNodeService、WorkloadService(28 方法)补方法级注释
- [x] 2.2 SvcService(8)、GroupService(7)、ConfigMapService(7)、AuditService(4)、AuthService(4)补注释
- [x] 2.3 AccountService(11)、ClusterSyncSettingService、YamlTemplateService、RedirectManager 等其余 Services 补注释
- [x] 2.4 Services/Identity(ChineseIdentityErrorDescriber 7 方法、IdentityRevalidatingAuthenticationStateProvider、IdentityComponentsEndpointRouteBuilderExtensions)补注释
- [x] 2.5 构建:Services 目录 CS1591 清零

## 3. Data 层注释

- [x] 3.1 Data/Repositories 4 个仓库类(21 方法)补中文注释
- [x] 3.2 Data/Entities 7 个实体类:类型级 + 关键字段注释(含 FK/级联/唯一索引等 schema 约束)
- [x] 3.3 ApplicationDbContext:类型级 + 关键配置注释
- [x] 3.4 构建:Data 目录 CS1591 清零

## 4. Requests 全量注释

- [x] 4.1 Requests/ 31 个文件:每个 *Request 类型级 summary + 属性注释(哨兵语义重点:ClusterQueryRequest)
- [x] 4.2 ClusterQueryRequest.cs 英文注释翻中文(与 cluster-query-layering spec 对照)
- [x] 4.3 构建:Requests 目录 CS1591 清零

## 5. ViewModels 注释

- [x] 5.1 ViewModels/ 26 个根目录文件:类型级中文 summary
- [x] 5.2 ViewModels 属性:按 CS1591 覆盖口径逐个补注释(纯数据字段可用简洁中文)
- [x] 5.3 ViewModels/Mappings 7 个扩展类:类型级注释(既有语义注释保留,不强制逐方法)
- [x] 5.4 构建:ViewModels 目录 CS1591 清零

## 6. 全量验证与文档同步

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx`:0 错误、0 条 CS1591、XML 文档文件产出
- [x] 6.2 `dotnet test MultiClusterMgmtSys.Tests`:全绿(基线 438)
- [x] 6.3 风格审计:`///` 后缺空格 0 命中;cluster-query-layering spec 语义对照抽查
- [x] 6.4 更新 AGENTS.md:Code style 节新增 XML 注释约定(范围/语言/风格/豁免口径)
- [x] 6.5 sync specs 到主 specs(code-style)
