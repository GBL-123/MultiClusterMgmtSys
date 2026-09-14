## 1. 凭据分类条组件重写

- [x] 1.1 在 `ClusterOverviewCard.razor` 用私有枚举 `CredentialState { Redacted, Loading, Revealed }` + `editForSecret` 缓存替换 `showSecret` / `showSecretContent` 双布尔,并删除 header 的「显示密文/隐藏密文」按钮;验证 = `dotnet build MultiClusterMgmtSys.slnx` 0 错误且全仓无 `showSecret|显示密文` 残留引用
- [x] 1.2 仅在 `AuthorizeView Roles="Admin"` 且 `Cluster.ConnectionType` 非 null 时渲染「连接凭据」分类条:条头显示类型(配置文件 (Kubeconfig) / 访问令牌 (Token)),隐藏态 `.credential-redacted` 占位 + 唯一「查看凭据」开关,展开态 `.credential-viewer` 等宽只读块 + 「复制」「隐藏」;验证 = 组件测试断言各状态自有类与按钮文案
- [x] 1.3 接线懒加载与失败回退:点击「查看凭据」→ Loading(`// 正在加载...`、开关禁用)→ `GetClusterForEditAsync` 成功后 Revealed;隐藏仅切状态不重取;失败经 `ExceptionPresenter` 提示并回 Redacted 不渲染原文;验证 = bUnit 测试断言再次展开时服务仅被调用一次(可计数的 mock/仓储)
- [x] 1.4 「复制」调 `navigator.clipboard.writeText` 复制原文并 snackbar「已复制到剪贴板」,`JSException` 走 `ExHandler.HandleAsync(ex, "复制")`;验证 = 组件测试断言 JS 调用与提示,服务层不新增方法

## 2. 样式

- [x] 2.1 `wwwroot/css/app.css` 新增 `.credential-annex`(顶部发丝线 + 上间距)、`.credential-redacted`(虚线等宽占位框、次文字色、左对齐)、`.credential-viewer`(等宽只读块,`max-height: 320px`、`overflow: auto`、`white-space: pre-wrap` + `word-break: break-all`);验证 = `dotnet build` 通过,且未引入新颜色/琥珀色
- [x] 2.2 用长 kubeconfig 与超长 Token 手动核对:展开态最小高度 320px 且填满剩余页面高度(YAML tab 同款 flex 链路)、内容可滚动、隐藏/展开切换无布局跳动;验证 = 本地运行 `dotnet run --project MultiClusterMgmtSys` 目视检查

## 3. 测试更新

- [x] 3.1 更新 `ClusterCardsTests.Admin_can_reveal_secret_via_service`:点击「查看凭据」后断言 `.credential-viewer` 含 `token-secret-cluster` 且出现「隐藏」;验证 = 该测试通过
- [x] 3.2 更新 `ClusterCardsTests.Member_has_no_secret_button`:断言 markup 不含「查看凭据」与 `.credential-annex`;验证 = 该测试通过
- [x] 3.3 新增隐藏态用例:展开前 markup 不含 token 原文,且 `Cluster.ConnectionType == null` 时整条不渲染;验证 = 新增测试通过
- [x] 3.4 跑 `dotnet test MultiClusterMgmtSys.Tests` 全量确认全绿且测试数量基线(600)按新增用例上浮不下降;验证 = 命令行输出

## 4. 验收

- [x] 4.1 对照 `openspec/changes/cluster-credential-display/specs/cluster-detail/spec.md` 逐条场景确认覆盖(隐藏态无泄漏、单一开关、懒加载缓存、复制、加载/失败、Member 不可见);验证 = 每条场景均有对应实现或测试
