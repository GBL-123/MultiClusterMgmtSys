# fallback-pages Delta

## Purpose

定义系统兜底页面契约:未匹配路由与 404 状态统一落到中文 not-found 页,服务端异常落到中文错误页;两页使用工业印刷词汇(发丝线卡片、mono 代码行)、提供明确返回入口,不保留 Blazor 模板英文文案与开发环境说明。

## ADDED Requirements

### Requirement: 404 兜底页
系统 SHALL 提供中文 404 兜底页:直接访问 `/not-found` 或经 `UseStatusCodePagesWithReExecute("/not-found")` 重执行时,页面 SHALL 渲染中文标题「页面不存在」、mono 代码行(404 标识)、简短中文说明与「返回集群列表」入口;SHALL 使用 `EmptyLayout` 与 `.fallback-page` 发丝线卡片(3px 圆角、无阴影);SHALL NOT 出现模板英文文案(如 "Not Found"/"Sorry, the content you are looking for does not exist.")。

#### Scenario: 直接访问兜底路由
- **WHEN** 用户访问 `/not-found`
- **THEN** 页面以中文渲染,含 404 mono 标识与「返回集群列表」按钮

#### Scenario: 未匹配路由落到兜底页
- **WHEN** 用户访问不存在的应用路由
- **THEN** Router 的 `NotFoundPage` 渲染同一中文兜底页,不显示模板英文文案

#### Scenario: 返回入口
- **WHEN** 用户点击「返回集群列表」
- **THEN** 导航到 `/clusters`(未登录时按既有认证链路跳转登录)

### Requirement: 错误兜底页
系统 SHALL 提供中文错误页:经 `UseExceptionHandler("/Error")` 重执行或直接访问 `/Error` 时,页面 SHALL 渲染中文标题「请求处理出错」、简短说明与「返回集群列表」入口;存在请求 ID(`Activity.Current?.Id` 或 `HttpContext.TraceIdentifier`)时 SHALL 以 mono 行展示;SHALL 使用 `EmptyLayout` 与 `.fallback-page` 词汇,布局与 404 兜底页一致;SHALL NOT 出现模板英文文案与开发环境(`ASPNETCORE_ENVIRONMENT`/Development)说明段落。

#### Scenario: 错误页中文渲染
- **WHEN** 管道重执行到 `/Error`
- **THEN** 页面为中文文案与工业词汇,无英文模板残留与开发环境说明

#### Scenario: 请求 ID 展示
- **WHEN** 请求 ID 存在
- **THEN** 页面以 mono 行展示该请求 ID;请求 ID 为空时不渲染该行

#### Scenario: 返回入口与布局
- **WHEN** 用户查看错误页
- **THEN** 页面使用 `EmptyLayout`,提供「返回集群列表」按钮,卡片词汇与 404 页一致
