# UniTaskPubSub

**work in progress**

UniTask & `IUniTaskAsyncEnumerable` baseed pub/sub messaging.
this is like the UniTask version of UniTask.MessageBroker.


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

messageBus.Receive<FooMessage>()
    .Subscribe(async foo => 
    {
        await LoadAsync(foo.Id);
        await ...
        await ...        
    });
    
//     
    
messageBus.Publish(new FooMessage { Id = 1 });
messageBus.Publish(new FooMessage { Id = 2 });
messageBus.Publish(new FooMessage { Id = 3 });
    
```

### Unity 2020.2+ + C# 8

```csharp
await foreach (var foo in messageBus.Receive<FooMessage>())
{
    // Do something
}
```
