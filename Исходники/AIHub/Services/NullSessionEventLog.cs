namespace AIHub.Services;

public sealed class NullSessionEventLog : ISessionEventLog
{
    public string FilePath => string.Empty;

    public string SessionId { get; } = $"memory-{Guid.NewGuid():N}";

    public void Write(string type, object? payload = null)
    {
    }

    public void Dispose()
    {
    }
}
