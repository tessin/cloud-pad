using System;
using System.Threading;

namespace CloudPad.Internal
{
    class CancellationScope : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly EventHandler _clx;
        private readonly CancellationTokenRegistration _ctr;

        public CancellationToken Token => _cts.Token;

        public CancellationScope(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();

            if (cancellationToken.CanBeCanceled)
            {
                _ctr = cancellationToken.Register(() => _cts.Cancel());
            }

            _clx = LINQPad.RegisterCleanup((sender, e) => _cts.Cancel());
        }

        public void Dispose()
        {
            LINQPad.UnregisterCleanup(_clx);

            _ctr.Dispose();
            _cts.Dispose();
        }
    }
}
