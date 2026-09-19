# restyle-reconnect-modal Tasks

## 1. 组件标记与文案

- [x] 1.1 重写 `Web/Components/Layout/ReconnectModal.razor`:中文文案(标题「连接已中断」、说明与四态消息)、头部琥珀方牌 + mono 状态行、2px 琥珀进度线容器、`[刷新页面]` 次按钮(id `components-reload-button`,复用失败态可见类);`dialog#components-reconnect-modal`、`#components-reconnect-button`、`#components-resume-button`、`#components-seconds-to-next-attempt`、全部 `components-reconnect-*`/`components-pause*`/`components-resume-failed` 状态类与 `@Assets` 脚本引用逐字保留;验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误 + 人工对照原文件确认契约 id/类名零丢失
- [x] 1.2 在 `ReconnectModal.razor.js` 绑定 `components-reload-button` → `location.reload()`(与现有监听同风格,不引入内联脚本);验证:模块加载无空引用异常(运行冒烟时检查控制台)

## 2. 样式迁移(app.css)

- [x] 2.1 在 `wwwroot/css/app.css` 设计语言区新增重连样式段:墨色淡化遮罩(无渐变)、纸卡 + 1px 发丝线 + 3px 圆角 + 无阴影、琥珀方牌、mono 状态行、2px 琥珀不确定进度线、墨底主按钮与发丝线次按钮、琥珀焦点环与 `scale(0.98)` 按压、各状态显隐(含 failed/resume-failed/paused/repeated-attempt)、窄屏自适应;验证:`dotnet build` 通过 + `dotnet run` 开发态断线弹窗样式生效(此前 scoped 包 500 不再影响)
- [x] 2.2 删除 `Web/Components/Layout/ReconnectModal.razor.css`,处理 `Components/App.razor` 的 `MultiClusterMgmtSys.Web.styles.css` 链接:确认删除后是否仍生成该 scoped 包,不再生成则移除该 `<link>`(无需保留空占位);验证:`dotnet run --project MultiClusterMgmtSys.Web` 启动无异常、登录页 200

## 3. 运行冒烟

- [ ] 3.1 运行冒烟:启动应用并登录,制造断线(停掉应用进程再启动 / 断网)观察弹窗——中文文案、方牌与进度线、失败态 `[重试]`+`[刷新页面]` 可见、点击刷新整页重载、恢复后弹窗自动关闭;验证:人工观察记录,无控制台报错
- [x] 3.2 暂停/恢复态可见性核对(若可复现):`[恢复会话]` 仅在 paused/resume-failed 出现,其余态不渲染可见按钮;验证:人工观察或按 CSS 规则静态核对

## 4. 文档

- [x] 4.1 AGENTS.md「UI / CSS conventions」补一句:重连弹窗样式位于 `app.css`(非 scoped CSS),原生对话框/按钮例外与框架 JS 契约 id 保持不变;验证:grep 到该说明且无旧表述残留

## 5. 回归

- [x] 5.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 5.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 733)
- [x] 5.3 `./coverage.ps1` 四程序集合并行覆盖率 ≥75%(本 change 不新增代码,数值应与基线持平)

## 6. 范围扩展:脚本迁移与兜底页面(用户确认并入)

- [x] 6.1 将模块脚本 `git mv` 至 `Web/wwwroot/js/reconnect.js`,组件脚本引用改为 `@Assets["js/reconnect.js"]`(id/状态类契约不变);验证:`dotnet run` 下登录页脚本 tag 的指纹 URL 返回 200(dev-run 静态资产 500 怪癖不再影响)
- [x] 6.2 重写 `Pages/NotFound.razor` 为中文 + 工业词汇(mono 404 标识、「页面不存在」、说明、「返回集群列表」),保留 `/not-found` 路由与 `EmptyLayout`;`app.css` 新增 `.fallback-page` 样式段;验证:build + 运行态 `/not-found` 200 且含中文文案
- [x] 6.3 重写 `Pages/Error.razor` 为中文 + 与 404 一致的卡片词汇,保留请求 ID mono 行、移除开发环境说明段落,保留 `/Error` 路由与 `EmptyLayout`;验证:build + 运行态 `/Error` 200 且无英文模板残留
- [x] 6.4 AGENTS.md UI 节补记:重连模块脚本位于 `wwwroot/js/reconnect.js`;兜底页(404/Error)中文 + `.fallback-page` 词汇;验证:grep 到说明
- [x] 6.5 回归重跑:`dotnet build` 0 错误 + `dotnet test` 全绿(733)+ `./coverage.ps1` ≥75%
