# Aoiro

Aoiro 是一个面向 Unity 的轻量级运行时架构库，提供模块生命周期、Command/Query、类型事件、Signal、数据绑定与统一取消令牌。

## 目录

- `Architecture`：架构入口、模块注册、能力接口与 Command/Query。
- `Events`：Signal、QueuedSignal、EventBus 与订阅令牌。
- `Binding`：BindableProperty 与只读接口。
- `Cancellation`：统一取消令牌与 GameObject 生命周期扩展。

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

多字段事件使用命名结构体，不提供多参数 Signal。

## BindableProperty

```csharp
var level = new BindableProperty<int>(1);
var token = level.Subscribe(value => UnityEngine.Debug.Log(value), invokeImmediately: true);
level.Value = 2;
level.SetValueWithoutNotify(3);
```

比较器属于单个 BindableProperty 实例，可通过构造函数传入。对外只读暴露时使用 `IReadOnlyBindableProperty<T>`。

## 取消令牌

所有框架令牌实现 `ICancelToken`。订阅使用 `SubscriptionToken`，重复取消安全：

```csharp
token.CancelOnDisable(gameObject);
token.CancelOnDestroy(gameObject);
gameObject.CancelAllTokens();
```

批量取消会尝试处理全部令牌；存在异常时最后抛出 `AggregateException`。
