# UniTaskPubSub

**work in progress**

UniTask & `IUniTaskAsyncEnumerable` baseed pub/sub messaging.
this is like the UniTask version of UniTask.MessageBroker.

## Installation

### Requirements

- Unity 2018.4+
- [Cysharp/UniTask](https://github.com/Cysharp/UniTask) Ver.2.0+

### Install via UPM (using Git URL)

1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies": { line
    - ```json
      "jp.hadashikick.unitaskpubsub": "https://github.com/hadashiA/UniTaskPubSub.git?path=Assets/UniTaskPubSub",
      ```
3. UPM should now install the package.

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
