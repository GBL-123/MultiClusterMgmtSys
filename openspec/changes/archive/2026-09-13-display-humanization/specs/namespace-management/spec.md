## MODIFIED Requirements

### Requirement: 命名空间状态展示
命名空间状态 SHALL 以 `ui-theme` 的淡彩状态徽章展示:阶段为 `Active` 的命名空间显示「在线」样式,阶段为 `Terminating` 的命名空间显示「未知」样式,其余未识别阶段显示「未知」样式。按 `display-conventions`,徽章主行显示中文,相邻次行以等宽字体展示英文原阶段(`Active` / `Terminating`)。

#### Scenario: Active 命名空间
- **WHEN** 列表中某命名空间的 `.status.phase` 为 `Active`
- **THEN** 该行状态列显示在线徽章,相邻次行显示等宽字体的 `Active`

#### Scenario: Terminating 命名空间
- **WHEN** 列表中某命名空间的 `.status.phase` 为 `Terminating`
- **THEN** 该行状态列显示未知徽章,相邻次行显示等宽字体的 `Terminating`,且该行仍可查看详情
