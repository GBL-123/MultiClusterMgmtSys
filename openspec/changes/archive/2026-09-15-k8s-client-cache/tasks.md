# k8s-client-cache Tasks

## 1. 缓存实现与注册

- [x] 1.1 新增 `Services/IClusterClientCache.cs` + `Services/ClusterClientCache.cs`(singleton;`GetOrCreate(ClusterInfo)` 同步签名;凭据五项指纹哈希防注入;`IMemoryCache` 承载,滑动空闲过期 10 分钟 + 容量上限 100,驱逐回调释放客户端),`dotnet build` 0 错误
- [x] 1.2 `Program.cs` 注册 `AddSingleton<IClusterClientCache, ClusterClientCache>()`,紧邻既有 `Func<KubernetesClientConfiguration, IKubernetes>` 工厂,`dotnet build` 0 错误

## 2. 服务接入(机械替换)

- [x] 2.1 `ClusterService.ProbeAsync` 改走 `clientCache.GetOrCreate(entity)`,验证:`MultiClusterMgmtSys.Tests/Services/ClusterServiceTests` 与 `ClusterRefreshConcurrencyTests` 全绿(超时捕获断言原样通过)
- [x] 2.2 `ConfigMapService` 全部方法替换获取行,验证:`ConfigMapServiceTests` 全绿
- [x] 2.3 `SvcService` 全部方法替换获取行,验证:`SvcServiceTests` 全绿
- [x] 2.4 `WorkloadService` 全部方法替换获取行,验证:`WorkloadServiceTests` 与 `WorkloadServiceErrorTests` 全绿
- [x] 2.5 `NamespaceService` 全部方法替换获取行,验证:`NamespaceServiceTests` 全绿
- [x] 2.6 `ClusterNodeService` 全部方法替换获取行,验证:`ClusterNodeServiceTests` 全绿
- [x] 2.7 `EventService` 全部方法替换获取行(event-management change 新增的 K8s 服务,与既有模式一致),验证:`EventServiceTests` 全绿

## 3. 缓存自身测试(覆盖 spec 场景)

- [x] 3.1 复用与隔离:同集群二次调用工厂仅触发一次、不同集群条目互不共享(spec「客户端按集群复用」两场景)
- [x] 3.2 指纹重建:改凭据后下次调用用新客户端;编辑入口外字段变化(模拟 ApiServer 回填)同样触发重建(spec「凭据变更触发客户端重建」两场景)
- [x] 3.3 未命中经工厂创建:捕获 config 断言统一超时被施加(spec「新建客户端遵循既有工厂与超时契约」场景)
- [x] 3.4 驱逐释放与在途降级:空闲条目过期被释放;驱逐期间在途调用异常可被既有翻译链路捕获(spec「空闲条目过期释放」「驱逐与在途请求的降级安全」场景)
- [x] 3.5 探测与并发:探测轮复用缓存客户端;同客户端并发调用互不串扰(spec「探测路径复用缓存」「缓存客户端的并发共享安全」场景)

## 4. 全量回归

- [x] 4.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 4.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 600 + event-management 已入 658 + 新增缓存测试 9 个),确认无既有测试因客户端实例语义变化而失败
