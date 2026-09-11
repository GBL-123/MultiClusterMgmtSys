# add-service-management

## Why

系统已覆盖集群、节点、配置(ConfigMap)、工作负载的管理,但缺少 Kubernetes Service 管理。Service 是暴露面审计与流量排障的核心对象("谁对外开了端口"、"服务背后有没有活着的 Pod"),当前运维只能去各集群手敲 kubectl,与"多集群统一管控"的定位不符。

## What Changes

- 新增「服务管理」功能,路由 `/services`(列表)与 `/services/{ClusterId:int}/{Namespace}/{Name}`(详情),复用 ConfigMap 页面的集群选择侧栏与页面骨架。
- 列表页以**端口为一等公民**:每端口一行的等宽字体展示(kubectl 语法 + targetPort),新增「对外入口」列(LB 外部 IP / NodePort 编号 / ExternalName DNS);Headless 服务显示 `None`,ExternalName 显示 `[ 暂无 ]`。
- 详情页展示 YAML 视图、端口表、Endpoints(后端 Pod 地址与 Ready 状态);Endpoints 读取以 EndpointSlice API 为主,集群不支持(404)时降级到旧 Endpoints API。
- 支持创建(从 YAML 模板)、YAML 编辑、删除,均限 Admin。
- YAML 编辑采用「读回 existing + 不可变字段守卫 + Replace」策略:`clusterIP`/`clusterIPs`/`ipFamilies` 与现有值不同时,在调用 API 之前抛出中文校验异常。
- 侧边导航新增「网络管理」分组(`MudNavGroup`),内含「服务管理」入口,为将来 Ingress 等网络资源预留分组位。
- 新增 `AuditCategory.Service`(服务)审计类别,服务创建/修改/删除写审计。

## Capabilities

### New Capabilities

- `service-management`:K8s Service 的跨集群管理能力——导航入口、列表(端口重点展示 + 对外入口列)、详情(YAML/端口/Endpoints,含 EndpointSlice 降级读取)、YAML 创建/编辑(不可变字段守卫)、删除、审计记录。

### Modified Capabilities

- `audit-log`:审计事件写入与记录内容两条需求的枚举扩展——类别新增"服务",事件新增服务创建、修改(YAML 编辑)、删除。

## Impact

- **新增**:`Services/SvcService.cs`(服务层)、`Requests/Svc*.cs`(4 个请求对象)、`ViewModels/Svc*.cs`(列表/详情/端口/端点 VM)+ `ViewModels/Mappings/SvcMappingExtensions.cs`、`Components/Svcs/Pages|Shared`(页面与共享组件)、`Common/Enums/AuditCategory.cs`(新枚举值)。
- **修改**:`Components/Layout/Drawer.razor`(网络管理分组)、`Program.cs`(注册 `SvcService`)、`Services/ConfigMapService.cs` 不动(`BuildConfig` 模式复制,与现有各服务保持独立)。
- **测试**:`MultiClusterMgmtSys.Tests` 新增 Services/ViewModels 映射测试(K8s 调用走 Moq `*WithHttpMessagesAsync`,Endpoints 降级路径覆盖 404 → 旧 API)。
- 不涉及数据库 schema 变更、不涉及新增 NuGet 依赖(k8s `DiscoveryV1` 客户端已随 KubernetesClient 19 提供)。
