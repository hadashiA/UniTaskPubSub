using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace UniTaskPubSub.AsyncEnumerable
{
    public interface IAsyncEnumerablePublisher
    {
        void Publish<T>(T msg);
    }

    public interface IAsyncEnumerableReceiver
    {
        IUniTaskAsyncEnumerable<T> Receive<T>();
    }

    public class AsyncEnumerableMessageBus :
        IAsyncEnumerablePublisher,
        IAsyncEnumerableReceiver,
        IDisposable
    {
        sealed class Pipe<T> : IDisposable
        {
            public readonly ChannelWriter<T> Writer;
            public readonly IConnectableUniTaskAsyncEnumerable<T> MulticastSource;
            public readonly IDisposable Connection;

            public Pipe()
            {
                var channel = Channel.CreateSingleConsumerUnbounded<T>();
                Writer = channel.Writer;
                MulticastSource = channel.Reader.ReadAllAsync().Publish();
                Connection = MulticastSource.Connect();
            }

            public void Dispose()
            {
                Writer.TryComplete();
                Connection.Dispose();
            }
        }

        public static readonly AsyncEnumerableMessageBus Default = new AsyncEnumerableMessageBus();

        readonly IDictionary<Type, object> pipes = new Dictionary<Type, object>();
        bool disposed;

        public void Publish<T>(T command)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("AsyncEnumerableMessageBus");
            }

            object pipe;
            lock (pipes)
            {
                if (!pipes.TryGetValue(typeof(T), out pipe))
                {
                    return;
                }
            }
            ((Pipe<T>)pipe).Writer.TryWrite(command);
        }

        public IUniTaskAsyncEnumerable<T> Receive<T>()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("AsyncEnumerableMessageBus");
            }

            object pipe;
            lock (pipes)
            {
                if (!pipes.TryGetValue(typeof(T), out pipe))
                {
                    pipe = new Pipe<T>();
                    pipes.Add(typeof(T), pipe);
                }
            }
            return ((Pipe<T>)pipe).MulticastSource;
        }

        public void Dispose()
        {
            lock (pipes)
            {
                foreach (var entry in pipes)
                {
                    if (entry.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                disposed = true;
            }
        }
    }
}
