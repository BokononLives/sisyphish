namespace sisyphish.Tools;

public class RequestTracker
{
    private int _requestCount = 0;
    private readonly TaskCompletionSource _waitForAllRequestsToProcess = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void BeginRequest()
    {
        Interlocked.Increment(ref _requestCount);
    }

    public void EndRequest()
    {
        if (Interlocked.Decrement(ref _requestCount) == 0)
        {
            _waitForAllRequestsToProcess.TrySetResult();
        }
    }

    public Task WaitForAllRequestsToProcess()
    {
        return Volatile.Read(ref _requestCount) == 0
            ? Task.CompletedTask
            : _waitForAllRequestsToProcess.Task;
    }
}
