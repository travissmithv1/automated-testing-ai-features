namespace MetricsApi.Services;

public class RateLimiter
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly Queue<DateTime> _requestTimestamps = new();
    private static readonly int _maxRequestsPerMinute = 45;
    private static readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(1);

    public static async Task WaitForSlot()
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _windowDuration;

            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            if (_requestTimestamps.Count >= _maxRequestsPerMinute)
            {
                var oldestRequest = _requestTimestamps.Peek();
                var waitTime = oldestRequest + _windowDuration - now;
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime);
                }

                while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < now - _windowDuration)
                {
                    _requestTimestamps.Dequeue();
                }
            }

            _requestTimestamps.Enqueue(now);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
