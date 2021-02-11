using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniTaskPubSub.Tests
{
    public class AsyncMessageBusTest
    {
        [UnityTest]
        public IEnumerator Unicast() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var received = default(TestMessage);

            messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                received = msg;
            });

            await messageBus.PublishAsync(new TestMessage(100));

            Assert.That(received, Is.InstanceOf<TestMessage>());
            Assert.That(received.X, Is.EqualTo(100));
        });

        [UnityTest]
        public IEnumerator Multicast() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var receives = 0;

            messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Delay(500);
                receives += 1;
            });

            await messageBus.PublishAsync(new TestMessage(100));

            Assert.That(receives, Is.EqualTo(2));
        });

        [UnityTest]
        public IEnumerator Cancellation() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var cts = new CancellationTokenSource();
            var receives = 0;

            messageBus.Subscribe<TestMessage>(async (msg, cancellation) =>
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellation);
                receives += 1;
            });

            messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            cts.Cancel();
            try
            {
                await messageBus.PublishAsync(new TestMessage(100), cts.Token);
            }
            catch (OperationCanceledException) {}
            Assert.That(receives, Is.Zero);
        });

        [UnityTest]
        public IEnumerator DisposeSubscription() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var receives = 0;

            var subscription1 = messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            var subscription2 = messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            subscription2.Dispose();
            await messageBus.PublishAsync(new TestMessage(100));

            Assert.That(receives, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator DisposeAll() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var receives = 0;

            messageBus.Subscribe<TestMessage>(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            messageBus.Dispose();

            var disposedException = default(ObjectDisposedException);
            try
            {
                await messageBus.PublishAsync(new TestMessage(100));
            }
            catch (ObjectDisposedException ex)
            {
                disposedException = ex;
            }

            Assert.That(disposedException, Is.InstanceOf<ObjectDisposedException>());
            Assert.That(receives, Is.Zero);
        });

        [UnityTest]
        public IEnumerator SubscribeWithSync() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncMessageBus();
            var receives = 0;

            messageBus.Subscribe<TestMessage>(msg =>
            {
                receives += 1;
            });

            await messageBus.PublishAsync(new TestMessage(100));
            Assert.That(receives, Is.EqualTo(1));
        });
    }
}