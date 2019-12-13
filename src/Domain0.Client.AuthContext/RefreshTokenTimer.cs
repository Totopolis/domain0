using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Domain0.Api.Client
{
    internal class RefreshTokenTimer : IDisposable
    {
        private const int DEFAULT_MIN_AWAIT_TIME = 50;
        private const int EXCEPTION_MIN_AWAIT_TIME = 15000;

        public RefreshTokenTimer(AuthenticationContext domain0AuthenticationContext)
        {
            authContext = domain0AuthenticationContext;
            _refreshLoopTask = Task.Run(RefreshLoop, cts.Token);
            currentMinimumAwaitTime = DEFAULT_MIN_AWAIT_TIME;
        }

        private Task _refreshLoopTask;

        private DateTime? nextRefreshTime;
        public DateTime? NextRefreshTime
        {
            get
            {
                using(nextRefreshTimeLock.ReaderLock())
                {
                    return nextRefreshTime;
                }
            }
            internal set
            {
                using (nextRefreshTimeLock.WriterLock())
                {
                    nextRefreshTime = value;
                }

                try
                {
                    refreshTimeChangedEvent.Release();
                }
                catch (SemaphoreFullException) { }
                catch (ObjectDisposedException) { }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            _refreshLoopTask.Wait();
            refreshTimeChangedEvent.Dispose();
        }

        private async Task RefreshLoop()
        {
            var ct = cts.Token;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var sleepTime = CalculateSleepTime();

                    var nextRefreshTimeChanged = await refreshTimeChangedEvent.WaitAsync(sleepTime, ct);
                    if (nextRefreshTimeChanged)
                        continue;

                    try
                    {
                        await authContext.Refresh();
                        currentMinimumAwaitTime = DEFAULT_MIN_AWAIT_TIME;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"Can't auto refresh: {ex.Message}");
                        currentMinimumAwaitTime = EXCEPTION_MIN_AWAIT_TIME;
                    }
                }
            }
            catch(OperationCanceledException)
            {
            }
            catch(Exception ex)
            {
                Trace.TraceError($"Unexpected exception in RefreshTokenTimer {ex}");
            }
        }

        private int CalculateSleepTime()
        {
            var nextTime = NextRefreshTime;

            if (nextTime.HasValue)
            {
                return Math.Max(
                    currentMinimumAwaitTime,
                    (int)(nextTime.Value - DateTime.UtcNow).TotalMilliseconds);
            }

            // infinity wait for change of NextRefreshTime
            return Timeout.Infinite;
        }

        private readonly AuthenticationContext authContext;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private int currentMinimumAwaitTime;
        private readonly AsyncReaderWriterLock nextRefreshTimeLock = new AsyncReaderWriterLock();
        private readonly SemaphoreSlim refreshTimeChangedEvent = new SemaphoreSlim(0, 1);
    }
}