## 1. 展示映射层与共享展示组件(基础)

- [x] 1.1 新增 `ViewModels/Mappings/K8sDisplayText.cs`(纯静态映射:节点状态/角色/阶段/地址类型/污点效果/条件类型/条件状态/工作负载条件/Service 类型/Endpoints 状态/账号角色/连接方式/命名空间阶段,CSS 类 helper,未登记值回退原文,中文 XML 注释);验证 = `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 1.2 新增映射层单测(覆盖全部映射项、多角色顿号连接、未登记回退原文,以及被收编的 `GetNodeStatusClass`/`GetRolloutText`/`GetConditionClass` 等价行为);验证 = 新增测试通过
- [x] 1.3 新增 `Components/Common/StackedText.razor`(中文主行 + 可选等宽次行 caption)与接线测试;验证 = 主/次行渲染断言通过
- [x] 1.4 新增 `Components/Common/StatusBadge.razor`(`.status-badge` + 圆点 + 中文主行 + 可选 raw 次行/tooltip),替换节点/命名空间/服务端点处重复的徽章标记;验证 = 徽章类名与双行渲染断言通过
- [x] 1.5 `app.css` 覆盖 `.mud-tooltip.mud-tooltip-default` 与箭头(墨底 `#111111`/纸字 `#F4F4F0`/3px/无投影)并提供等宽内容类;新增 `Components/Common/TextTooltip.razor`(统一 Delay/Placement/焦点,支持 Inline 与 Mono);验证 = tooltip 契约测试断言渲染类名与内容通过
- [x] 1.6 统一 `TooltipIconButton`(统一延迟/箭头/位置,保留 `Text` 作 tooltip 与 `aria-label`、点击收起)并更新其测试;验证 = `TooltipIconButtonTests` 全绿

## 2. 节点域改造

- [x] 2.1 `ClusterNodeViewModel`/`ClusterNodeDetailViewModel`/`NodeAddressViewModel`/`NodeTaintViewModel`/`NodeConditionViewModel` 增加计算型 `*Text`/`*CssClass`(raw 字段保留)并在 `NodeOverviewCard` 的 Phase 标签改为「阶段 (Phase)」;验证 = `ClusterNodeServiceTests` 新增展示字段断言通过
- [x] 2.2 `NodeOverviewCard` 状态/角色/阶段/地址类型/污点效果改为主中文 + 次英文行;验证 = `NodeListTableTests`/页面测试双语渲染断言通过
- [x] 2.3 `NodeConditionsCard` 类型/状态双语(Reason/Message 保持原文);`NodeSystemInfoCard` 十个字段标签改为「中文 (English)」;验证 = bUnit 渲染断言涵盖 `Ready`/`MemoryPressure`/`Architecture` 等
- [x] 2.4 `NodeResourcesCard` 原始值由原生 `title` 改为统一 tooltip(容量/可分配两列);验证 = 悬停内容含 `3800m`/`16297496Ki` 的接线断言通过
- [x] 2.5 `NodeListTable` 状态/角色双语、`ClusterNodesCard` 同步;`NodeListFilterBar` 角色/状态选项改为「中文 (原值)」且提交值保持 raw;验证 = `NodeListTableTests`、`PageFilterFlowTests` 更新后全绿

## 3. Service 域改造

- [x] 3.1 `SvcListViewModel`/`SvcDetailViewModel`/`SvcPortViewModel`/`SvcEndpointViewModel` 增加 `*Text`/`*CssClass` 计算属性;验证 = `SvcMappingTests` 新增断言通过
- [x] 3.2 `SvcListTable` 类型列双语、空态改「[ 暂无服务 ]」;`SvcListFilterBar` 类型选项「中文 (原值)」(提交 `ClusterIP` 等 raw);验证 = `SvcTableTests`/筛选流测试通过
- [x] 3.3 `SvcEndpointTable` 状态徽章双语、卡片与 tab 标签「后端端点 (Endpoints)」;`SvcDetail.razor` 页面标题「服务详情」;验证 = 详情页渲染断言通过

## 4. 工作负载域改造

- [x] 4.1 `WorkloadListViewModel.ReadyText` 加「个」;`WorkloadConditionViewModel` 增加 `*Text`;验证 = `WorkloadMappingTests` 更新后全绿
- [x] 4.2 `WorkloadListTable` 就绪度 `n/m 个`;`WorkloadStatusCard` 条件表类型/状态双语(`UID` 标签保持原文);验证 = `WorkloadTableTests` 更新后全绿

## 5. 账号与个人资料改造

- [x] 5.1 `AccountViewModel` 增加 `RoleText`;`AccountTable` 角色列双语;`AccountEditDialog`/批量改角色选项「管理员 (Admin)/成员 (Member)」(提交值 raw);验证 = `AccountTableTests`/`AccountDialogTests` 更新后全绿
- [x] 5.2 `Profile.razor` 角色徽章双语;验证 = 个人资料渲染断言通过

## 6. 集群与命名空间改造

- [x] 6.1 `ClusterViewModel`/`ClusterDetailViewModel` 增加 `ConnectionTypeText`/`NodeCountText`;`ClusterOverviewCard` 连接方式与节点数按新口径展示;验证 = `ClusterCardsTests` 更新后全绿
- [x] 6.2 `NamespaceListViewModel`/`NamespaceDetailViewModel` 暴露原始 phase 供次行;`NamespaceListTable`/`NamespaceDetailToolbar` 状态徽章补英文次行;验证 = `NamespacesPageTests` 更新后全绿

## 7. 缺陷修复与 tooltip 全站替换

- [x] 7.1 `AuditLogMappingExtensions.ToDisplayName` 补 `Service`/`Namespace` 中文类别并加测试(现有 spec 已要求,属实现缺陷);验证 = 审计类别映射测试通过
- [x] 7.2 替换全部原生 `title`(`NodeAnnotationsCard`、`NodeLabelsCard`、`NamespaceAnnotationsCard`、`NamespaceLabelsCard`、`NodeConditionsCard`、`Profile` 审计目标、`SvcListTable` 端口行、`NodeResourcesCard` 已含于 2.4)为 `TextTooltip`;`AccountTable` 直接 `MudTooltip` 改用统一组件;验证 = 代码检索无用户可见 `title=` 残留
- [x] 7.3 删除组件内 `GetNodeStatusClass`/`GetRolloutText/Class`/`GetConditionClass` 重复 helper,改为消费 `K8sDisplayText`;验证 = 代码检索零残留且构建 0 错误

## 8. 回归与验收

- [x] 8.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误;`dotnet test MultiClusterMgmtSys.Tests` 全绿且测试数不低于 526
- [x] 8.2 CS1591 零命中、连续成员行静态审计零命中;验证 = 构建输出与检索结果
- [x] 8.3 对照 `openspec/specs/display-conventions`、`ui-theme`、`node-detail-layout`、`nodes-page`、`service-management`、`workload-management`、`accounts-page`、`profile-page`、`cluster-detail`、`namespace-management` 的场景逐条确认覆盖;验证 = 每条场景有实现或测试对应
- [ ] 8.4 (可选)运行应用手工冒烟:节点详情资源/条件/系统信息、Service 列表与详情、工作负载条件、账号角色、审计类别、tooltip 悬停样式;验证 = 观察结果与 spec 场景一致
