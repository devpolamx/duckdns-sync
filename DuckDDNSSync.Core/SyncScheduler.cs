namespace DuckDDNSSync.Core
{
    public class SyncScheduler
    {
        private CancellationTokenSource? _cts;

        public event Func<Task>? Tick;

        public void Start(TimeSpan interval)
        {
            Stop();
            var cts = new CancellationTokenSource();
            _cts = cts;
            _ = RunLoop(interval, cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task RunLoop(TimeSpan interval, CancellationToken token)
        {
            await RaiseTick();
            try
            {
                while (true)
                {
                    await Task.Delay(interval, token);
                    await RaiseTick();
                }
            }
            catch (OperationCanceledException)
            {
                // detenido intencionalmente (Stop/Start)
            }
        }

        private async Task RaiseTick()
        {
            if (Tick != null) await Tick.Invoke();
        }
    }
}
