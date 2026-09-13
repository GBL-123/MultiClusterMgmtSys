## ADDED Requirements

### Requirement: 账号角色双语展示
账号列表的角色列 SHALL 遵循 `display-conventions` 以中文主行 + 英文原值次行展示:`Admin` → 管理员、`Member` → 成员。角色选择控件(编辑账号、批量改角色)SHALL 以中文为主、原值对照展示,提交给服务层的值 SHALL 保持 `Admin`/`Member` 原始字符串。角色徽章的淡彩配色 SHALL 保持 `ui-theme` 既有 admin/member 词汇。

#### Scenario: 角色列双语
- **WHEN** 账号表渲染角色为 `Member` 的行
- **THEN** 角色徽章主行显示「成员」,相邻次行显示等宽字体的 `Member`

#### Scenario: 角色选择项双语
- **WHEN** Admin 打开编辑账号对话框的角色选择
- **THEN** 选项显示「管理员 (Admin)」「成员 (Member)」
- **AND** 提交给服务层的角色值仍为原始 `Admin` / `Member`
