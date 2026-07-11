using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ScenarioSessionLogTests
{
    [TestMethod]
    public void CreateUncertainty_UsesUniqueFilesAndFlushesOnDispose()
    {
        var root = Path.Combine(Path.GetTempPath(), "AIHubTests", Guid.NewGuid().ToString("N"));
        try
        {
            var settings = new StorageSettings
            {
                Results = new StorageCategorySettings
                {
                    Locations = [new StorageLocationSettings { Path = root }]
                }
            };
            string firstPath;
            string secondPath;
            using (var first = ScenarioSessionLog.CreateUncertainty(settings))
            using (var second = ScenarioSessionLog.CreateUncertainty(settings))
            {
                firstPath = first.FilePath;
                secondPath = second.FilePath;
                first.Write("test_event", new { Value = 42 });
            }

            Assert.AreNotEqual(firstPath, secondPath);
            var line = File.ReadAllLines(firstPath).Single();
            using var document = JsonDocument.Parse(line);
            Assert.AreEqual(ScenarioSessionLog.CurrentSchemaVersion, document.RootElement.GetProperty("SchemaVersion").GetInt32());
            Assert.AreEqual("test_event", document.RootElement.GetProperty("Type").GetString());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
