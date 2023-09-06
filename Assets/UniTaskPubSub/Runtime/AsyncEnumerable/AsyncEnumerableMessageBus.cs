using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace UniTaskPubSub.AsyncEnumerable
{
    public interface IAsyncEnumerablePublisher
    {
        UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default);
    }

    public interface IAsyncEnumerableSubscriber
    {
        IUniTaskAsyncEnumerable<T> Receive<T>();
        IDisposable Subscribe<T>(Func<T, CancellationToken, UniTask> action, CancellationToken cancellation = default);
    }

    public static class AsyncPublisherExtensions
    {
        public static void Publish<T>(
            this IAsyncEnumerablePublisher publisher,
            T msg,
            CancellationToken cancellation = default)
        {
            publisher.PublishAsync(msg, cancellation).Forget();
        }
    }

    class SubscribeAsyncEnumerable<T> : MoveNextSource, IUniTaskAsyncEnumerable<AsyncUnit>, IUniTaskAsyncEnumerator<AsyncUnit>
    {
        readonly IUniTaskAsyncEnumerator<T> source;
        readonly Func<T, CancellationToken, UniTask> action;
        readonly CancellationToken cancellation;

        public SubscribeAsyncEnumerable(
            IUniTaskAsyncEnumerator<T> source,
            Func<T, CancellationToken, UniTask> action,
            CancellationToken cancellation = default)
        {
            this.source = source;
            this.action = action;
            this.cancellation = cancellation;
        }

        public AsyncUnit Current => AsyncUnit.Default;

        public IUniTaskAsyncEnumerator<AsyncUnit> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return this;
        }

        public UniTask DisposeAsync()
        {
            return UniTask.CompletedTask;
        }

        public async UniTask<bool> MoveNextAsync()
        {
            cancellation.ThrowIfCancellationRequested();
            if (await source.MoveNextAsync())
            {
                await action(source.Current, cancellation);
                return true;
            }
            return false;
        }
    }

    public class AsyncEnumerableMessageBus : IAsyncEnumerablePublisher, IAsyncEnumerableSubscriber
    {
        class Pipe<T> : IDisposable
        {
            public readonly IConnectableUniTaskAsyncEnumerable<T> MulticastSource;

            readonly Channel<T> channel = Channel.CreateSingleConsumerUnbounded<T>();
            readonly IDisposable connection;
            readonly List<SubscribeAsyncEnumerable<T>> subscribers = new();

            public Pipe()
            {
                MulticastSource = channel.Reader.ReadAllAsync().Publish();
                connection = MulticastSource.Connect();
            }

            public Subscription<T> AddSubscriber(
                Func<T, CancellationToken, UniTask> action,
                CancellationToken cancellation = default)
            {
                var subscriber = new SubscribeAsyncEnumerable<T>(
                    MulticastSource.GetAsyncEnumerator(),
                    action,
                    cancellation);
                lock (subscribers)
                {
                    subscribers.Add(subscriber);
                }
                return new Subscription<T>(this, subscriber);
            }

            public void RemoveSubscriber(SubscribeAsyncEnumerable<T> subscriber)
            {
                lock (subscribers)
                {
                    subscribers.Remove(subscriber);
                }
            }

            public async UniTask WriteAsync(T msg, CancellationToken cancellation = default)
            {
                channel.Writer.TryWrite(msg);
                foreach (var subscription in subscribers)
                {
                    await subscription.MoveNextAsync();
                }
            }

            public void Dispose()
            {
                channel.Writer.TryComplete();
                connection.Dispose();
            }
        }

        readonly struct Subscription<T> : IDisposable
        {
            public readonly Pipe<T> Pipe;
            public readonly SubscribeAsyncEnumerable<T> Subscriber;

            public Subscription(Pipe<T> pipe, SubscribeAsyncEnumerable<T> subscriber)
            {
                Pipe = pipe;
                Subscriber = subscriber;
            }

            public void Dispose()
            {
                Subscriber.DisposeAsync();
                Pipe.RemoveSubscriber(Subscriber);
            }
        }

        readonly IDictionary<Type, object> pipes = new Dictionary<Type, object>();
        bool disposed;

        public UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default)
        {
            if (disposed) throw new ObjectDisposedException("AsyncEnumerableMessageBus");

            Pipe<T> pipe = null;
            lock (pipes)
            {
                if (pipes.TryGetValue(typeof(T), out var entry))
                {
                    pipe = (Pipe<T>)entry;
                }
            }

            if (pipe == null)
            {
                return UniTask.CompletedTask;
            }
            return pipe.WriteAsync(msg, cancellation);
        }

        public IUniTaskAsyncEnumerable<T> Receive<T>()
        {
            if (disposed) throw new ObjectDisposedException("AsyncEnumerableMessageBus");

            return GetOrCreatePipe<T>().MulticastSource;
        }

        public IDisposable Subscribe<T>(
            Func<T, CancellationToken, UniTask> action,
            CancellationToken cancellation = default)
        {
            if (disposed) throw new ObjectDisposedException("AsyncEnumerableMessageBus");
            return GetOrCreatePipe<T>().AddSubscriber(action, cancellation);
        }

        Pipe<T> GetOrCreatePipe<T>()
        {
            lock (pipes)
            {
                if (pipes.TryGetValue(typeof(T), out var entry))
                {
                    return (Pipe<T>)entry;
                }
                var pipe = new Pipe<T>();
                pipes.Add(typeof(T), pipe);
                return pipe;
            }
        }
    }
}
