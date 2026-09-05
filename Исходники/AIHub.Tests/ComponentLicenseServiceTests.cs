using System.Text.Json;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ComponentLicenseServiceTests
{
    private string _root = "";
    [TestInitialize] public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "lopata-license-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        WriteCatalog("terms-1");
    }
    [TestCleanup] public void Cleanup() => Directory.Delete(_root, true);
    private void WriteCatalog(string terms) => File.WriteAllText(Path.Combine(_root, "catalog.json"),
        JsonSerializer.Serialize(new[] { new ComponentLicenseEntry { Id = "test", Terms = terms } }));
    private ComponentLicenseService Service() => new(_root, Path.Combine(_root, "receipts.json"));

    [TestMethod] public async Task DeclineDoesNotCreateReceipt()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service().EnsureAsync(["test"], _ => Task.FromResult(false), default));
        Assert.IsFalse(File.Exists(Path.Combine(_root, "receipts.json")));
    }
    [TestMethod] public async Task RestartDoesNotAskAgainAndChangedTermsDo()
    {
        var count = 0;
        Task<bool> Accept(IReadOnlyList<ComponentLicenseEntry> _) { count++; return Task.FromResult(true); }
        await Service().EnsureAsync(["test"], Accept, default);
        await Service().EnsureAsync(["test"], Accept, default);
        Assert.AreEqual(1, count);
        WriteCatalog("terms-2");
        await Service().EnsureAsync(["test"], Accept, default);
        Assert.AreEqual(2, count);
    }
    [TestMethod] public async Task InstallerReceiptIsRecognized()
    {
        File.WriteAllText(Path.Combine(_root, "installer-receipts.json"), JsonSerializer.Serialize(new[] {
            new ComponentLicenseReceipt("test", "terms-1", DateTimeOffset.UtcNow, "installer", "1") }));
        await Service().EnsureAsync(["test"], _ => throw new AssertFailedException("Already accepted in installer"), default);
    }
    [TestMethod] public async Task CorruptReceiptRequiresAcknowledgement()
    {
        File.WriteAllText(Path.Combine(_root, "receipts.json"), "broken");
        var count = 0;
        await Service().EnsureAsync(["test"], _ => { count++; return Task.FromResult(true); }, default);
        Assert.AreEqual(1, count);
    }
    [TestMethod] public async Task UnknownComponentDoesNotGetBlanketConsent()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().EnsureAsync(["unknown"], _ => Task.FromResult(true), default));
        Assert.IsFalse(File.Exists(Path.Combine(_root, "receipts.json")));
    }
    [TestMethod] public async Task CancellationWhileDialogOpenDoesNotSave()
    {
        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Service().EnsureAsync(["test"], _ => {
            cts.Cancel(); return Task.FromResult(true); }, cts.Token));
        Assert.IsFalse(File.Exists(Path.Combine(_root, "receipts.json")));
    }
    [TestMethod] public async Task ConcurrentRequestsAskOnce()
    {
        var count = 0;
        var service = Service();
        async Task<bool> Accept(IReadOnlyList<ComponentLicenseEntry> _) { count++; await Task.Delay(10); return true; }
        await Task.WhenAll(service.EnsureAsync(["test"], Accept, default), service.EnsureAsync(["test"], Accept, default));
        Assert.AreEqual(1, count);
    }
    [TestMethod] public async Task FailedWriteDoesNotPretendToPersist()
    {
        var path = Path.Combine(_root, "occupied");
        Directory.CreateDirectory(path);
        var service = new ComponentLicenseService(_root, path);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.EnsureAsync(["test"], _ => Task.FromResult(true), default));
        Assert.HasCount(0, service.ReadReceipts());
    }
}
