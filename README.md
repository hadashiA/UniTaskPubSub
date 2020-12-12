# UniTaskPubSub

**work in progress**

`IUniTaskAsyncEnumerable` base pub/sub messaging.


## Usage

```csharp
struct FooMessage
{
    public int Id;
}
```

```csharp
using UniTaskPubSub;

var messageBus = new MessageBus();

messageBus.Publish(new FooMessage { Id = 1 });

messageBus.Receive<FooMessage>()
    .Subscribe(async foo => 
    {
        await LoadAsync(foo.Id);
        await ...
    });
```

### Unity 2020.2+ + C# 8

```csharp
await foreach (var foo in messageBus.Receive<FooMessage>())
{
    // Do something
}
```
