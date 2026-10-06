public sealed class AutoSaveWriteGate
{
    private float lastRequestTime;
    private float nextAttemptTime;

    public bool Pending { get; private set; }

    public void Request(float now)
    {
        Pending = true;
        lastRequestTime = now;
        nextAttemptTime = now;
    }

    public bool IsDue(float now, float debounceSeconds)
    {
        return Pending && now >= nextAttemptTime && now - lastRequestTime >= debounceSeconds;
    }

    public void MarkSaved()
    {
        Pending = false;
    }

    public void MarkFailed(float now, float retryDelaySeconds)
    {
        Pending = true;
        nextAttemptTime = now + retryDelaySeconds;
    }
}
