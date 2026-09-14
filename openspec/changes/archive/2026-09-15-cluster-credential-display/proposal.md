## Why

集群详情页概览 tab 的密文展示用 `MudTextField ReadOnly` + `InputType.Password` 承载:多行 kubeconfig 被渲染成整块圆点墙、只读数据却长着可编辑表单的样子、双层开关(header「显示密文」+ 框内眼睛)语义重复、展开内容以分隔线悬挂在信息网格之外、且没有复制入口。管理员既读不清也取不走凭据,是本页可用性最差的一块。

## What Changes

- 将密文区从「header 开关 + ReadOnly 遮蔽 `MudTextField` + 可见性装饰」重设计为概览卡内的**连接凭据分类条**:
  - 隐藏态:虚线等宽占位框(`[ 密文已隐藏 · 仅管理员可见 ]`),条头显示凭据类型(`配置文件 (Kubeconfig)` / `访问令牌 (Token)`,来自 `Cluster.ConnectionTypeText`,无需等接口)与「查看凭据」按钮。
  - 展开态:等宽只读内容块(受 `max-height` 约束可滚动,KubeConfig 按 YAML 原文、Token 长串换行),条头提供「复制」与「隐藏」。
- 单一开关:仅「查看凭据 / 隐藏」控制显隐,删除双层遮蔽与眼睛装饰;「复制」走 `navigator.clipboard.writeText`,与端点卡同口径(成功提示「已复制到剪贴板」)。
- 保留既有安全与加载语义:`AuthorizeView Roles="Admin"` 门控、`GetClusterForEditAsync` 首次展开懒加载一次并缓存、隐藏不重取、加载中显示 `// 正在加载...`、加载失败经 `ExceptionPresenter` 提示并保持收起。
- 删除概览卡 header 的「显示密文/隐藏密文」按钮。
- 新增凭据块样式(等宽只读块 + 虚线隐藏框),复用既有设计 token,不引入新颜色。
- 不改服务层、仓库、数据库 schema 与路由;不新增 NuGet 依赖。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `cluster-detail`: 重写「Show-secret toggle reuses `GetClusterForEditAsync` as-is」要求——密文展示从「header 开关 + 只读遮蔽 `MudTextField` + 可见性装饰 + 双层开关」改为「概览卡连接凭据分类条:隐藏占位(含类型)/单一开关/等宽只读展开块/复制/隐藏」;服务调用路径、Admin-only 门控、懒加载与缓存语义保持不变。

## Impact

- **修改**:`MultiClusterMgmtSys/Components/Clusters/Shared/ClusterOverviewCard.razor`(密文区重写、header 按钮移除、`MudTextField`/`showSecret` 双标志收敛为单状态机);`MultiClusterMgmtSys/wwwroot/css/app.css`(新增凭据块样式)。
- **测试**:`MultiClusterMgmtSys.Tests/Components/Clusters/ClusterCardsTests.cs`(`Admin_can_reveal_secret_via_service` 改按「查看凭据」驱动并断言展开块;`Member_has_no_secret_button` 改断言无凭据入口;新增隐藏态不含密文与复制按钮接线用例)。
- **规格**:`openspec/specs/cluster-detail/spec.md`(同步 delta)。
- **不涉及**:`ClusterService.GetClusterForEditAsync` 契约、`ClusterEditViewModel`、权限模型、审计、K8s 调用。
