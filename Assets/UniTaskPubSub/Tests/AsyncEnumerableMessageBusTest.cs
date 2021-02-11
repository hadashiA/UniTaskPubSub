#if UNITY_2019_4_OR_NEWER
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UniTaskPubSub.AsyncEnumerable;

namespace UniTaskPubSub.Tests
{
    readonly struct TestMessage
    {
        public readonly int Id;

        public TestMessage(int x)
        {
            Id = x;
        }
    }

    public class AsyncEnumerableMessageBusTest
    {
        [UnityTest]
        public IEnumerator Multicast() => UniTask.ToCoroutine(async () =>
        {
            var messageBus = new AsyncEnumerableMessageBus();
            var receives = 0;

            messageBus.Receive<TestMessage>()
                .SelectAwait(async msg =>
                {
                    await UniTask.Yield();
                    return msg.Id;
                })
                .Subscribe(_ => receives += 1);

            messageBus.Receive<TestMessage>()
                .Subscribe(_ => receives += 1);

            messageBus.Publish(new TestMessage(100));

            await UniTask.Delay(500);

            Assert.That(receives, Is.EqualTo(2));
        });

        [Test]
        public void DisposeAll()
        {
            var messageBus = new AsyncEnumerableMessageBus();
            var receives = 0;

            messageBus.Receive<TestMessage>().Subscribe(async msg =>
            {
                await UniTask.Yield();
                receives += 1;
            });

            messageBus.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                messageBus.Publish(new TestMessage(100));
            });
            Assert.That(receives, Is.Zero);
        }
    }
}
#endif