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
