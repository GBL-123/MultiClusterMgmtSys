# unit-testing

## ADDED Requirements

### Requirement: 新增代码覆盖率要求

所有新增或修改的代码行覆盖率 SHALL ≥75%：新代码 SHALL 随附单元测试（服务层按服务边界写，组件按接线契约写）；开发者 SHALL 通过覆盖率报告逐文件自查新增文件的行覆盖。无 CI 现状下，全局棘轮阈值兜住整体稀释；精确按 diff 的"新增代码"检查待 CI 落地后升级。

#### Scenario: 新代码随附测试

- **WHEN** 新增服务方法或组件逻辑
- **THEN** 同一 change 内新增对应测试，覆盖率报告中该文件行覆盖 ≥75%

#### Scenario: 逐文件自查

- **WHEN** 开发者完成一个功能的验证
- **THEN** 以覆盖率验证命令的报告确认本次改动文件的行覆盖达标

### Requirement: AGENTS.md 测试约定

AGENTS.md SHALL 记录：`dotnet test` 命令、覆盖率验证命令（含当前棘轮阈值与最新实测基线）、测试目录镜像结构、服务边界测试口径、bUnit 只测接线契约的口径、TestInfrastructure 用法、覆盖率规范（新增代码 ≥75%、棘轮只升不降）。

#### Scenario: 命令与口径文档化

- **WHEN** 查看 AGENTS.md
- **THEN** Commands 含 `dotnet test` 与覆盖率验证命令，Testing 节描述上述约定与覆盖率规范
