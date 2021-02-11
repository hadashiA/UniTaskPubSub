using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniTaskPubSub.Internal;

namespace UniTaskPubSub
{
    public interface IAsyncPublisher
    {
        UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default);
    }

    public interface IAsyncSubscriber
    {
        IDisposable Subscribe<T>(
            Func<T, CancellationToken, UniTask> action,
            CancellationToken cancellation = default);
    }

    public static class AsyncPublisherExtensions
    {
        public static void Publish<T>(
            this IAsyncPublisher publisher,
            T msg,
            CancellationToken cancellation = default)
        {
            publisher.PublishAsync(msg, cancellation).Forget();
        }
    }

    public static class AsyncSubscriberExtensions
    {
        public static IDisposable Subscribe<T>(
            this IAsyncSubscriber subscriber,
            Func<T, UniTask> action)
        {
            return subscriber.Subscribe<T>((msg, cancellation) => action(msg));
        }

        public static IDisposable Subscribe<T>(
            this IAsyncSubscriber subscriber,
            Action<T> action)
        {
            return subscriber.Subscribe<T>((msg, cancellation) =>
            {
                action(msg);
                return UniTask.CompletedTask;
            });
        }
    }

    public class AsyncMessageBus : IAsyncPublisher, IAsyncSubscriber, IDisposable
    {
        sealed class Pipe<T>
        {
            readonly List<Func<T, CancellationToken, UniTask>> subscribers =
                new List<Func<T, CancellationToken, UniTask>>();

            public void AddSubscriber(Func<T, CancellationToken, UniTask> subscriber)
            {
                lock (subscribers)
                {
                    subscribers.Add(subscriber);
                }
            }

            public void RemoveSubscriber(Func<T, CancellationToken, UniTask> subscriber)
            {
                lock (subscribers)
                {
                    subscribers.Remove(subscriber);
                }
            }

            public UniTask FireAllAsync(T msg, CancellationToken cancellation = default)
            {
                UniTask[] buffer;
                lock (subscribers)
                {
                    buffer = CappedArrayPool<UniTask>.Shared8Limit.Rent(subscribers.Count);
                    for (var i = 0; i < subscribers.Count; i++)
                    {
                        buffer[i] = subscribers[i](msg, cancellation);
                    }
                }
                try
                {
                    return UniTask.WhenAll(buffer);
                }
                finally
                {
                    CappedArrayPool<UniTask>.Shared8Limit.Return(buffer);
                }
            }
        }

        sealed class Subscription<T> : IDisposable
        {
            readonly AsyncMessageBus parent;
            readonly Func<T, CancellationToken, UniTask> subscriber;

            public Subscription(AsyncMessageBus parent, Func<T, CancellationToken, UniTask> subscriber)
            {
                this.parent = parent;
                this.subscriber = subscriber;
            }

            public void Dispose()
            {
                Pipe<T> pipe = null;
                lock (parent.pipes)
                {
                    if (parent.pipes.TryGetValue(typeof(T), out var entry))
                    {
                        pipe = (Pipe<T>)entry; }
                }
                pipe?.RemoveSubscriber(subscriber);
            }
        }

        readonly IDictionary<Type, object> pipes = new Dictionary<Type, object>();

        bool disposed;

        public async UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default)
        {
            Pipe<T> pipe = null;
            lock (pipes)
            {
                if (disposed)
                    throw new ObjectDisposedException("AsyncMessageBus");

                if (pipes.TryGetValue(typeof(T), out var entry))
                {
                    pipe = (Pipe<T>)entry;
                }
            }

            if (pipe == null)
            {
                return;
            }

            await pipe.FireAllAsync(msg, cancellation);
        }

        public IDisposable Subscribe<T>(
            Func<T, CancellationToken, UniTask> action,
            CancellationToken cancellationToken = default)
        {
            Pipe<T> pipe;
            lock (pipes)
            {
                if (disposed)
                    throw new ObjectDisposedException("AsyncMessageBus");

                if (pipes.TryGetValue(typeof(T), out var entry))
                {
                    pipe = (Pipe<T>)entry;
                }
                else
                {
                    pipe = new Pipe<T>();
                    pipes.Add(typeof(T), pipe);
                }
                pipe.AddSubscriber(action);
            }
            return new Subscription<T>(this, action);
        }

        public void Dispose()
        {
            lock (pipes)
            {
                pipes.Clear();
                disposed = true;
            }
        }
    }
}