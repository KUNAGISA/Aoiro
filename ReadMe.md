# Aoiro

> A lightweight runtime architecture library for Unity.

`Aoiro` 是一个面向 Unity 的轻量级运行时架构库，提供统一的架构入口、模块注册、命令与查询分发，以及常用的事件、信号和数据绑定能力。

它不是一个“大而全”的框架，而是一套更适合放在项目核心层的基础设施：足够轻，足够清晰，也足够直接。

---

## ✦ 名字来源

`Aoiro` 这个名字来源于西尾维新的《戏言系列》中角色“玖渚友”的外号“青色学者”。

这里取“青色”之意，作为这个库的名字，同时也是框架的根命名空间：`Aoiro`。

---

## ✦ 设计参考

本库整体设计参考并借鉴了 [QFramework](https://github.com/liangxiegame/QFramework) 的架构思路。

在此基础上，Aoiro 做了更偏向项目运行时核心层的裁剪与简化，重点保留了：

- 清晰的 `Architecture` 统一入口
- 明确的 `Model / System / Service` 职责划分
- 轻量直接的 `Command / Query` 调用方式
- 更容易按需组合的基础运行时模块

---

## ✦ 核心特性

- `Architecture<TArchitecture>` 作为统一架构入口
- `Model`、`System`、`Service` 三类模块的注册与生命周期管理
- `Command` 与 `Query` 的统一分发
- `IOCContainer` 运行时模块存取
- `EventBus` 类型安全事件派发
- `Signal` 轻量订阅与取消
- `BindableProperty<T>` 可观察数据绑定
- `CancelToken` 统一取消句柄

---

## ✦ 架构说明

### Architecture

`Architecture<TArchitecture>` 是整个框架的核心。

一个 Architecture 实例负责：

- 注册和获取模块
- 初始化和销毁模块
- 分发命令与查询
- 注册和派发事件

### 模块职责

- `Model`：承载共享状态与运行时数据
- `System`：承载业务流程与跨模块协调逻辑
- `Service`：承载基础设施能力或外部服务封装
- `Command`：承载无状态写操作
- `Query`：承载无状态读操作

通常建议让状态尽量收敛在 `Model`，让流程进入 `System`，让外部依赖隔离在 `Service`。

---

## ✦ 快速开始

### 1. 定义自己的 Architecture

```csharp
using Aoiro;

public sealed class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void OnInit()
    {
        Register(new PlayerModel());
        Register(new PlayerSystem());
        Register(new SaveService());
    }

    protected override void OnDestroy()
    {
    }
}
```

### 2. 定义 Model

```csharp
using Aoiro;

public sealed class PlayerModel : AbstractModel
{
    public readonly BindableProperty<int> Level = new(1);
}
```

### 3. 定义 Command

```csharp
using Aoiro;

public sealed class LevelUpCommand : AbstractCommand
{
    protected override void Execute(ICommandContext context)
    {
        var playerModel = context.GetModel<PlayerModel>();
        playerModel.Level.Value++;
    }
}
```

### 4. 发送 Command

```csharp
GameArchitecture.Instance.SendCommand(new LevelUpCommand());
```

---

## ✦ 常用能力

### 获取模块

```csharp
var playerModel = GameArchitecture.Instance.Get<PlayerModel>();
```

也可以在支持扩展能力的上下文中直接调用：

```csharp
var playerModel = this.GetModel<PlayerModel>();
var saveService = this.GetService<SaveService>();
```

### EventBus

适合按事件类型进行广播：

```csharp
public struct PlayerLevelChanged
{
    public int Level;
}

var token = GameArchitecture.Instance.RegisterEvent<PlayerLevelChanged>(e =>
{
    UnityEngine.Debug.Log(e.Level);
});

GameArchitecture.Instance.SendEvent(new PlayerLevelChanged
{
    Level = 2
});
```

### BindableProperty

适合对单个值进行观察：

```csharp
var token = playerModel.Level.RegisterWithInitValue(level =>
{
    UnityEngine.Debug.Log(level);
});

playerModel.Level.Value = 2;
```

### Signal

适合更轻量的本地信号机制：

```csharp
var signal = new Signal<int>();
var token = signal.Register(value =>
{
    UnityEngine.Debug.Log(value);
});

signal.Emit(100);
```

---

## ✦ 使用约定

- `Command` 和 `Query` 设计为无状态对象；如果调用频率高，建议使用结构体或对象复用
- 一个 `Architecture` 内部应尽量保持模块职责单一，避免相互污染
- `System` 更适合组织流程，`Model` 更适合承载状态，`Service` 更适合封装能力
- 当不再需要整个架构实例时，可以调用 `DestroyInstance()` 释放模块与事件
- `Instance` 在实例为空时会按需创建新架构；`DestroyInstance()` 只销毁当前实例，不提供终止状态保护
- `MonoBehaviour.OnDestroy`、`OnDisable` 等销毁回调应尽量避免访问 `Instance`、获取模块或发送 Command/Event，优先清理自身持有的 Token、事件订阅和缓存引用
- 销毁回调确需访问架构时使用 `TryGetInstance`；获取失败后应跳过架构操作，不得再访问 `Instance`
