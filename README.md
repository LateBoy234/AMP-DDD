# AMP-DDD

以自动测量软件为业务背景，逐步学习领域建模与框架设计。

本项目按“一次一个类”的节奏演进，每次说明对象的职责、设计原因与适用边界。目前只有领域层及其测试，尚未接入现场设备。

## 目录

- `src/AMP.Domain`：计量、运动位置等领域概念与业务规则。
- `tests/AMP.Domain.Tests`：领域规则测试。
- `docs`：随代码编写的中文学习文档。

从 [第 1 步：从位置理解领域对象](docs/01-从位置理解领域对象.md) 开始阅读，对应代码为 [Position3D](src/AMP.Domain/Motion/Position3D.cs)。目录中同时保留了已有的计量领域代码。

## 构建与测试

需要 .NET 10 SDK，在仓库根目录执行：

```powershell
dotnet restore AMP.slnx
dotnet build AMP.slnx --no-restore
dotnet test tests/AMP.Domain.Tests/AMP.Domain.Tests.csproj --no-restore
```

当前测试覆盖计量领域规则，`Position3D` 已通过编译验证。
