using AIHub.Services;

namespace AIHub.Tests;

internal static class TestComponentManagerFactory
{
    private static readonly string EmptyStatePath = Path.Combine(
        Path.GetTempPath(),
        "AIHub.Tests",
        $"empty-component-state-{Environment.ProcessId}-{Guid.NewGuid():N}.json");

    public static ComponentManager CreateEmpty() =>
        new(new ComponentStateStore(EmptyStatePath));

    public static ExecutionRoutePlannerService CreateEmptyRoutePlanner() =>
        new(new CapabilityResolverService(CreateEmpty()));
}
