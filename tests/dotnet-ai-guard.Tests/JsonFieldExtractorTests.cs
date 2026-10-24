using System.Text.Json.Nodes;

namespace AiGuard.Tests;

public class JsonFieldExtractorTests
{
    private const string Messages = """{"messages":[{"role":"user","content":"hi"},{"role":"user","content":"there"}]}""";
    private static readonly string[] ContentPath = ["messages[*].content"];
    private static readonly string[] PromptPath = ["prompt"];
    private static readonly string[] NumberPath = ["n"];
    private static readonly string[] Greetings = ["hi", "there"];
    private static readonly string[] Prompt = ["x"];

    [Fact]
    public void Extract_WildcardArrayPath_ReturnsEveryContentInOrder()
    {
        var values = JsonFieldExtractor.Extract(Messages, ContentPath);

        Assert.Equal(Greetings, values);
    }

    [Fact]
    public void Extract_TopLevelProperty_ReturnsStringValue()
    {
        var values = JsonFieldExtractor.Extract("""{"prompt":"x","n":3}""", PromptPath);

        Assert.Equal(Prompt, values);
    }

    [Fact]
    public void Extract_NonStringProperty_ReturnsEmpty()
    {
        var values = JsonFieldExtractor.Extract("""{"prompt":"x","n":3}""", NumberPath);

        Assert.Empty(values);
    }

    [Fact]
    public void Extract_InvalidJson_ReturnsEmptyWithoutThrowing()
    {
        var values = JsonFieldExtractor.Extract("{not json", PromptPath);

        Assert.Empty(values);
    }

    [Fact]
    public void Rewrite_WildcardArrayPath_UppercasesEveryContentAndKeepsOtherProperties()
    {
        var rewritten = JsonFieldExtractor.Rewrite(Messages, ContentPath, s => s.ToUpperInvariant());

        var root = JsonNode.Parse(rewritten)!;
        var messages = root["messages"]!.AsArray();
        Assert.Equal("HI", messages[0]!["content"]!.GetValue<string>());
        Assert.Equal("THERE", messages[1]!["content"]!.GetValue<string>());
        Assert.Equal("user", messages[0]!["role"]!.GetValue<string>());
        Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        Assert.Equal(2, messages.Count);
    }
}
