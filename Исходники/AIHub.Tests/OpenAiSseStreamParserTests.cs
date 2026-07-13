using System.Collections.Concurrent;
using System.Text;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class OpenAiSseStreamParserTests
{
    [TestMethod]
    public async Task ReadAsync_AssemblesContentAndToolArguments()
    {
        const string sse = """
            data: {"choices":[{"delta":{"content":"Привет "},"finish_reason":null}]}

            data: {"choices":[{"delta":{"content":"мир"},"finish_reason":null}]}

            data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"web_","arguments":"{\"q\":"}}]},"finish_reason":null}]}

            data: {"choices":[{"delta":{"tool_calls":[{"index":0,"function":{"name":"search","arguments":"\"test\"}"}}]},"finish_reason":"tool_calls"}]}

            data: [DONE]

            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sse));
        var chunks = new ConcurrentQueue<ModelStreamChunk>();

        var result = await OpenAiSseStreamParser.ReadAsync(
            stream,
            new Progress<ModelStreamChunk>(chunks.Enqueue),
            CancellationToken.None);
        await Task.Delay(50);

        Assert.AreEqual("Привет мир", result.Content);
        Assert.AreEqual("tool_calls", result.FinishReason);
        Assert.AreEqual("web_search", result.ToolCalls.Single().Function.Name);
        Assert.AreEqual("{\"q\":\"test\"}", result.ToolCalls.Single().Function.Arguments);
        Assert.IsTrue(chunks.Any(chunk => chunk.Text.Contains("Привет", StringComparison.Ordinal)));
        Assert.IsTrue(chunks.Last().IsComplete);
    }
}
