using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

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
            public readonly List<Func<T, CancellationToken, UniTask>> Subscribers =
                new List<Func<T, CancellationToken, UniTask>>();
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
                lock (parent.pipes)
                {
                    if (parent.pipes.TryGetValue(typeof(T), out var entry))
                    {
                        var pipe = (Pipe<T>)entry;
                        pipe.Subscribers.Remove(subscriber);
                    }
                }
            }
        }

        public static readonly AsyncMessageBus Default = new AsyncMessageBus();

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

            var awaiters = new UniTask[pipe.Subscribers.Count];
            for (var i = 0; i < pipe.Subscribers.Count; i++)
            {
                awaiters[i] = pipe.Subscribers[i](msg, cancellation);
            }

            try
            {
                await UniTask.WhenAll(awaiters);
            }
            catch (OperationCanceledException) {}
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
                pipe.Subscribers.Add(action);
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