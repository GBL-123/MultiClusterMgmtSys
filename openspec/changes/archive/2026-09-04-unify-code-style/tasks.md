## 1. C# 成员空行(Services)

- [x] 1.1 `Services/` 全部 .cs(ClusterService、ClusterNodeService、ConfigMapService、GroupService、AccountService、AuditService、AuthService、ExceptionPresenter 等):字段/属性/方法间补空行,每批后 build
- [x] 1.2 `Common/Exceptions/`、`Common/Enums/` 检查并统一

## 2. C# 成员空行(ViewModels / Data / Requests / Models)

- [x] 2.1 `ViewModels/**`(含 Mappings 除外)、`Components/Clusters/ViewModels/`:Cluster* 系列 ~20 个文件成员间补空行
- [x] 2.2 `Data/Entities/`、`Data/Repositories/`、`Requests/**`、`Models/**`:成员间补空行
- [x] 2.3 全量 `dotnet build` 0 错误

## 3. Razor 注入迁移(@inject 顶部化)

- [x] 3.1 迁移 12 个文件的 `[Inject]` → 顶部 `@inject`(ClusterDetail、NodeDetail、Nodes、ConfigMaps、ConfigMapDetail、EditConfigMapYaml、CreateConfigMapDialog、Profile、ResetPasswordDialog、AccountEditDialog、EditClusterDialog、AccountTable 相关页等),修复迁移处缩进错乱,同名去重
- [x] 3.2 该批 build 0 错误

## 4. Razor 参数注解与成员间隔

- [x] 4.1 全部 .razor 的 `@code`:`[Parameter]`/`[CascadingParameter]` 拆为独立行,属性/方法之间空行(约 40 个文件,按组件目录分批)
- [x] 4.2 该批 build 0 错误

## 5. 审计与文档

- [x] 5.1 运行"连续成员行"静态审计脚本(探索阶段口径),除类首行成员外零命中;人工抽查 5-8 个文件目检
- [x] 5.2 AGENTS.md 新增 Code style 节(razor 注入顶部化/参数注解独立行/成员间空行/审计方式)
- [x] 5.3 `dotnet build` 最终 0 错误