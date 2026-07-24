using System.Text.Json;
using AIHub.Models;
using AIHub.Services;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AIHub.Tests;

[TestClass]
public sealed class SessionFileToolServiceTests
{
    [TestMethod]
    public void ExecutorCatalog_CombinesWebAndSessionFileTools()
    {
        var tools = ExecutorToolCatalog.CreateDefinitions(
            includeWeb: true,
            includeSessionFiles: true);
        var names = tools.Select(tool => tool.Function.Name).ToArray();

        CollectionAssert.Contains(names, "web_search");
        CollectionAssert.Contains(names, "session_files_list");
        CollectionAssert.Contains(names, "session_file_inspect");
        CollectionAssert.Contains(names, "session_file_read");
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void ListFiles_DoesNotExposePathOrContent()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "private.txt");
            File.WriteAllText(path, "private-content");
            var manifest = CreateManifest(path);

            var result = new SessionFileToolService().ListFiles(manifest);

            Assert.IsFalse(result.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.Contains("private-content", StringComparison.Ordinal));
            Assert.IsTrue(result.Contains("private.txt", StringComparison.Ordinal));
            Assert.IsTrue(result.Contains(manifest.Files[0].Id, StringComparison.Ordinal));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void ReadText_ReturnsBoundedChunksWithNextOffset()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "story.txt");
            File.WriteAllText(path, "abcdefghijklmnopqrstuvwxyz");
            var manifest = CreateManifest(path);
            var service = new SessionFileToolService();

            var firstJson = service.Read(
                manifest,
                manifest.Files[0].Id,
                offset: 0,
                maxCharacters: 10,
                CancellationToken.None);
            using var first = JsonDocument.Parse(firstJson);

            Assert.AreEqual("abcdefghij", first.RootElement.GetProperty("content").GetString());
            Assert.IsTrue(first.RootElement.GetProperty("has_more").GetBoolean());
            Assert.AreEqual(10, first.RootElement.GetProperty("next_offset").GetInt32());

            var secondJson = service.Read(
                manifest,
                manifest.Files[0].Id,
                offset: 10,
                maxCharacters: 10,
                CancellationToken.None);
            using var second = JsonDocument.Parse(secondJson);
            Assert.AreEqual("klmnopqrst", second.RootElement.GetProperty("content").GetString());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void ReadWordAndSpreadsheet_ExtractsActualContent()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var wordPath = Path.Combine(root, "brief.docx");
            using (var document = WordprocessingDocument.Create(
                       wordPath,
                       WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(
                    new Body(
                        new Paragraph(
                            new Run(
                                new Text("Verified document content")))));
                mainPart.Document.Save();
            }

            var sheetPath = Path.Combine(root, "data.xlsx");
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Facts");
                sheet.Cell(1, 1).Value = "Name";
                sheet.Cell(1, 2).Value = "Value";
                sheet.Cell(2, 1).Value = "Answer";
                sheet.Cell(2, 2).Value = 42;
                workbook.SaveAs(sheetPath);
            }

            var manifest = CreateManifest(wordPath, sheetPath);
            var service = new SessionFileToolService();
            var wordResult = service.Read(
                manifest,
                manifest.Files[0].Id,
                0,
                1_000,
                CancellationToken.None);
            var sheetResult = service.Read(
                manifest,
                manifest.Files[1].Id,
                0,
                1_000,
                CancellationToken.None);

            Assert.IsTrue(wordResult.Contains("Verified document content", StringComparison.Ordinal));
            Assert.IsTrue(sheetResult.Contains("Answer", StringComparison.Ordinal));
            Assert.IsTrue(sheetResult.Contains("42", StringComparison.Ordinal));
            Assert.IsFalse(wordResult.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(sheetResult.Contains(root, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void ReadImage_RejectsFalseSemanticAccess()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "photo.jpg");
            File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF, 0xD9]);
            var manifest = CreateManifest(path);
            var service = new SessionFileToolService();

            var exception = Assert.ThrowsExactly<SessionFileToolException>(() =>
                service.Read(
                    manifest,
                    manifest.Files[0].Id,
                    0,
                    1_000,
                    CancellationToken.None));

            Assert.AreEqual("content_adapter_unavailable", exception.Code);
            Assert.IsTrue(exception.SafeMessage.Contains("cannot understand image pixels", StringComparison.Ordinal));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task Gateway_UnknownFileIdReturnsSafeToolError()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "private.txt");
            File.WriteAllText(path, "secret");
            var manifest = CreateManifest(path);
            var call = new StructuredToolCall
            {
                Id = "call-1",
                Function = new StructuredToolCallFunction
                {
                    Name = "session_file_read",
                    Arguments = """{"file_id":"not-allowed"}"""
                }
            };

            using var log = new NullSessionEventLog();
            var execution = await new ExecutorToolGateway().ExecuteAsync(
                call,
                new StorageSettings(),
                manifest,
                log,
                CancellationToken.None);

            Assert.IsTrue(execution.Content.Contains("\"success\": false", StringComparison.Ordinal));
            Assert.IsTrue(execution.Content.Contains("file_not_allowed", StringComparison.Ordinal));
            Assert.IsFalse(execution.Content.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(execution.Content.Contains(path, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static SessionFileManifest CreateManifest(params string[] paths)
    {
        var manifest = new SessionFileManifest();
        new SessionFileManifestService().AddFiles(manifest, paths);
        return manifest;
    }

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), "AIHubSessionFileToolTests", Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
