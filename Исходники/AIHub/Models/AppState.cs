namespace AIHub.Models;

public sealed class AppState
{
    public bool HasCompletedSetup { get; set; }

    public DateTimeOffset? ComputerPassportLastUpdated { get; set; }
}
