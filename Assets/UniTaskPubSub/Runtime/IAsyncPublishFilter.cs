using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniTaskPubSub
{
    public interface IAsyncPublishFilter
    {
        UniTask PublishFilterAsync<T>(
            T msg,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> next);
    }
}
