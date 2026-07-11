namespace AIHub.Services;

public interface ISessionEventLog : IDisposable
{
    string FilePath { get; }

    string SessionId { get; }

    void Write(string type, object? payload = null);
}
