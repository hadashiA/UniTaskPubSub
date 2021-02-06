# UniTaskPubSub

UniTask & `IUniTaskAsyncEnumerable` based pub/sub messaging.
This is like the UniTask version of UniRx.MessageBroker.

## Pub/Sub (Publish/Subscribe) pattern

In the Pub/Sub messaging model, the sender who creates and sends a message is called the **publisher**. The party receiving the message is called the **subscriber**.


- Makes the sender and receiver loosely coupled, rather than the object directly calling the method.
- It is possible to create multiple processes for a single message. In other words, the number of subscribers to a publisher is 1:N.


## UniTask

[UniTask](https://github.com/Cysharp/UniTask) is a library that brings fast and powerful async/await to the Unity world.

UniTaskPubSub uses UniTask and allows async/await for processing pub/sub messages.

## Pub/Sub & Game archtecture

In game development, one event in the game often affects many factors and many objects. This is one of the reasons why game development is so complex.
It is useful to make the sender and receiver loosely coupled by a Pub/sub messaging system.

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

### UniTask based pub/sub

```csharp
struct FooMessage
{
    public int Id;
}
```

```csharp
var messageBus = new AsyncMessageBus();

// Subscriber

messageBus.Subscribe<FooMessage>(async msg =>
{
    await DoSomething1Async(msg.Id);
});

messageBus.Subscribe<FooMessage>(async msg =>
{
    await DoSomething2Async(msg.Id);
});


// Publisher

// Await for all subscribers.
await messageBus.PublishAsync(new FooMessage { Id = 1 });

// After PublishAsync awaited, DoSomething1Async and DoSomething2Async have been completed.
```

#### Fire & forget

When using Publish(), do not wait for Subscriber.

```csharp
messageBus.Publish(new FooMessage { Id = 1 });
```

#### CancellationToken

`PublishAsync` and `Publish` can be passed a cancellationToken.
This can be used to cancel the process registered in the Subscriber.

```csharp
messageBus.Subscribe<FooMessage>(async (msg, cancellationToken) =>
{
    // ...
    cancellationToken.ThrowIfCancellationRequested();
    // ...
});

await messageBus.PublishAsync(new FooMessage(), cancellationToken);
```

#### Subscription


`Subscribe` returns `IDisposable`. 
Disposing of this will unsubscribe.

```csharp
var subscription = messageBus.Subscribe<FooMessage>(...);
subscription.Dispose();
```

The `AddTo` extension to UniTask is useful.

```csharp
messageBus.Subscribe<FooMessage>(...)
    .AddTo(cancellationToken);
```


### IUniTaskAsyncEnumerable based pub/sub

*Required UniTask.Linq*

`AsyncEnumerableMessageBus` can subscribe to a message as an async stream.

`AsyncEnumerableMessageBus.Receive<TMessage>` returns `IUniTaskAsyncEnumerable`.
You can take full advantage of LINQ operations and Subscribe functions for IUniTaskAsyncEnumerable.

```csharp
struct FooMessage
{
    public int Id;
}
```

```csharp
using UniTaskPubSub;

var messageBus = new AsyncEnumerableMessageBus();

messageBus.Receive<FooMessage>()
    .Where(msg => msg.Id > 1)
    .Take(2)
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


In Unity 2020.2+ + C# 8, you can also use `await foreach`.

```csharp
await foreach (var foo in messageBus.Receive<FooMessage>())
{
    // Do something
}
```
