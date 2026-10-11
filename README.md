# Aoiro

Aoiro 是一个面向 Unity 的轻量级运行时架构库，提供模块生命周期、Command/Query、类型事件、Signal、数据绑定与统一取消令牌。

## 目录

- `Architecture`：架构入口、模块注册、能力接口与 Command/Query。
- `Events`：Signal、QueuedSignal、EventBus 与订阅令牌。
- `Binding`：BindableProperty 与只读接口。
- `Cancellation`：统一取消令牌与 GameObject 生命周期扩展。
- `Logging`：框架内部的静态日志入口。

## 内部日志

框架通过 `internal static class AoiroLog` 统一输出日志，其 `Log`、`LogWarning`、`LogError`、`LogException` 方法仅供框架内部使用。定义 `UNITY_5_3_OR_NEWER` 时调用对应的 `UnityEngine.Debug` 接口；其他环境使用 .NET 的 `Console`，普通日志输出到标准输出，警告、错误和异常输出到标准错误。异常保留完整信息及可用的堆栈。

## Architecture

```csharp
using Aoiro;

public sealed class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void OnInitialize()
    {
        RegisterModule(new PlayerModel());
        RegisterModule(new PlayerSystem());
        RegisterModule<ISaveService>(new SaveService());
    }

    protected override void OnShutdown()
    {
    }
}
```

`Instance` 会按需初始化架构，也可以显式调用 `Initialize()`。退出时调用 `Shutdown()`；模块按 Service、Model、System 的注册顺序初始化，并按 System、Model、Service 的反向顺序关闭。

## 模块

```csharp
public sealed class PlayerModel : ModelBase
{
    public BindableProperty<int> Level { get; } = new BindableProperty<int>(1);
}

public sealed class PlayerSystem : SystemBase
{
    protected override void OnInitialize()
    {
        var player = this.GetModel<PlayerModel>();
    }

    protected override void OnShutdown()
    {
    }
}
```

模块通过 `SystemBase`、`ModelBase`、`ServiceBase` 获得受限能力。System 默认不能访问其他 System；存在横向 System 依赖时，应在具体类型上显式实现 `ISystemAccessor`。框架外入口也应组合 `ISystemAccessor`、`ICommandExecutor`、`IEventPublisher` 等小接口，只暴露需要的权限。

## Command 与 Query

Command 和 Query 是轻量接口，频繁调用时优先使用结构体：

```csharp
public readonly struct LevelUpCommand : ICommand
{
    void ICommand.Execute(ICommandContext context)
    {
        context.GetModel<PlayerModel>().Level.Value++;
    }
}

public readonly struct GetLevelQuery : IQuery<int>
{
    int IQuery<int>.Execute(IQueryContext context)
    {
        return context.GetModel<PlayerModel>().Level.Value;
    }
}

GameArchitecture.Instance.ExecuteCommand(new LevelUpCommand());
var level = GameArchitecture.Instance.ExecuteQuery<GetLevelQuery, int>(new GetLevelQuery());
```

## 事件与 Signal

```csharp
var token = GameArchitecture.Instance.SubscribeEvent<PlayerLevelChanged>(OnLevelChanged);
GameArchitecture.Instance.PublishEvent(new PlayerLevelChanged(2));
token.Cancel();

var signal = new Signal<int>();
var localToken = signal.Subscribe(value => UnityEngine.Debug.Log(value));
signal.Emit(100);
```

`Signal` 禁止同实例重入；需要递归派发时使用 `QueuedSignal<T>`，它会按 FIFO 延后处理。EventBus 内部使用 QueuedSignal，因此同类型事件递归发布也是 FIFO。

`Signal` 与 `Signal<T>` 逐个捕获订阅者的同步异常，通过内部日志入口记录后继续通知其他订阅者，不自动取消失败订阅者，也不回滚其副作用。由订阅者触发的同实例重入异常同样被记录，外层派发继续。`QueuedSignal<T>` 在订阅者失败后继续处理排队事件；单轮超过 1024 次派发时仍清空待派发队列并抛出异常。发布者不能通过 `Emit` 是否抛出异常判断订阅者是否成功，异步回调恢复后的异常不在此隔离范围内。

多字段事件使用命名结构体，不提供多参数 Signal。

## BindableProperty

```csharp
var level = new BindableProperty<int>(1);
var token = level.Subscribe(value => UnityEngine.Debug.Log(value), invokeImmediately: true);
level.Value = 2;
level.SetValueWithoutNotify(3);
```

比较器属于单个 BindableProperty 实例，可通过构造函数传入。对外只读暴露时使用 `IReadOnlyBindableProperty<T>`。

属性更新通知遵循 Signal 的异常隔离契约。`invokeImmediately: true` 会先通知再注册订阅；立即通知失败时通过内部日志入口记录，随后仍正常注册并返回可取消的订阅令牌。通知失败不会回滚属性值，校验应在赋值入口完成。

## 取消令牌

所有框架令牌实现 `ICancelToken`。订阅使用 `SubscriptionToken`，重复取消安全。

以下 GameObject 生命周期扩展及其 Trigger 类仅在定义 `UNITY_5_3_OR_NEWER` 的 Unity 环境下编译；非 Unity 环境仍可使用 `ICancelToken`、`CancelToken<T>` 和 `SubscriptionToken`，通过 `Cancel()` 手动取消：

```csharp
token.CancelOnDisable(gameObject);
token.CancelOnDestroy(gameObject);
gameObject.CancelAllTokens();
```

批量取消会尝试处理全部令牌；存在异常时最后抛出 `AggregateException`。
