using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UniTaskPubSub.Tests
{
    class TestFilter1 : IAsyncPublishFilter
    {
        public int Invoked;

        public UniTask PublishFilterAsync<T>(
            T msg,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> next)
        {
            Invoked += 1;
            return next(msg, cancellation);
        }
    }

    class TestFilter2 : IAsyncPublishFilter
    {
        public int Invoked;

        public UniTask PublishFilterAsync<T>(
            T msg,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> next)
        {
            Invoked += 1;
            return next(msg, cancellation);
        }
    }

    class IgnoreMessageFilter : IAsyncPublishFilter
    {
        public UniTask PublishFilterAsync<T>(
            T msg,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> next)
        {
            return UniTask.CompletedTask;
        }
    }

    [TestFixture]
    public class AsyncPublishFilterTest
    {
        [UnityTest]
        public IEnumerator CallFilters() => UniTask.ToCoroutine(async () =>
        {
            var filter1 = new TestFilter1();
            var filter2 = new TestFilter2();
            var messageBus = new AsyncMessageBus(new IAsyncPublishFilter[] { filter1, filter2 });

            var subscribeCalls = 0;
            messageBus.Subscribe<TestMessage>(msg => subscribeCalls++);

            await messageBus.PublishAsync(new TestMessage(100));
            Assert.That(filter1.Invoked, Is.EqualTo(1));
            Assert.That(filter2.Invoked, Is.EqualTo(1));
            Assert.That(subscribeCalls, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator CallIgnoreFilter() => UniTask.ToCoroutine(async () =>
        {
            var ignoreFilter = new IgnoreMessageFilter();
            var messageBus = new AsyncMessageBus(new IAsyncPublishFilter[] { ignoreFilter });

            var subscribeCalls = 0;
            messageBus.Subscribe<TestMessage>(msg => subscribeCalls++);
            await messageBus.PublishAsync(new TestMessage(100));

            Assert.That(subscribeCalls, Is.EqualTo(0));
        });
    }
}