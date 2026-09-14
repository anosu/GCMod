using System.Text.Json;
using System.Text.Json.Nodes;
using GCMod.Services;
using Xunit;

namespace GCMod.Tests;

public class MasterDataTranslatorTests
{
    private static readonly MasterDataTranslator Translator = new(
        JsonSerializer.Deserialize<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        >(
            """
            {
              "mTest": {
                "ml_name[]": {"薬": "药", "EN": "不得替换英文"},
                "ml_description|": {"説明": "说明"},
                "name": {"名前": "名称", "未翻訳": "", "null翻訳": null},
                "text": {"一行\n二行": "第一行\n\"第二行\"\\<br>"}
              }
            }
            """
        )
    );

    [Fact]
    public void TranslatesThreeFormatsAndPreservesOtherData()
    {
        const string json = """
            [{"ml_name":["薬","EN"],"ml_description":"説明|English||",
              "name":"名前","id":9007199254740993,"nested":{"name":"名前"},"enabled":true}]
            """;
        var result = JsonNode.Parse(Translator.Translate("mTest", json, ["mTest"]));
        var expected = JsonNode.Parse(
            """
            [{"ml_name":["药","EN"],"ml_description":"说明|English||",
              "name":"名称","id":9007199254740993,"nested":{"name":"名前"},"enabled":true}]
            """
        );
        Assert.True(JsonNode.DeepEquals(expected, result));
    }

    [Theory]
    [InlineData("mTest", "another")]
    [InlineData("mtest", "mTest")]
    [InlineData("unknown", "unknown")]
    public void UnselectedOrMissingTablesReturnOriginalWithoutParsing(
        string tableName,
        string enabled
    )
    {
        const string json = "not JSON";
        Assert.Same(json, Translator.Translate(tableName, json, [enabled]));
        Assert.Same(json, Translator.Translate("mTest", json, []));
    }

    [Fact]
    public void WildcardEnablesKnownAndFutureTablesButMissingTranslationsRemainUnchanged()
    {
        Assert.True(MasterDataTranslator.IsEnabled("mFutureTable", ["*"]));
        Assert.False(MasterDataTranslator.IsEnabled(null, ["*"]));
        Assert.False(MasterDataTranslator.IsEnabled("mTest", []));
        Assert.True(MasterDataTranslator.IsEnabled("mTest", ["another", "*"]));

        string result = Translator.Translate("mTest", """{"name":"名前"}""", ["*"]);
        Assert.Equal("名称", JsonNode.Parse(result)["name"].GetValue<string>());

        const string unknown = "not JSON";
        Assert.Same(unknown, Translator.Translate("mFutureTable", unknown, ["*"]));
    }

    [Theory]
    [InlineData("[{\"name\":\"未翻訳\"}]")]
    [InlineData("[{\"name\":\"null翻訳\"}]")]
    [InlineData("[{\"name\":\"missing\"}]")]
    [InlineData("[{\"ml_name\":[]},{\"ml_name\":[null,\"EN\"]}]")]
    [InlineData("[{\"ml_name\":\"薬\",\"name\":42,\"ml_description\":null},null,5]")]
    [InlineData("[{}, {\"ml_name\":[\"missing\",\"EN\"]}]")]
    public void EmptyTranslationsMissingFieldsAndWrongTypesAreUnchanged(string json)
    {
        Assert.Same(json, Translator.Translate("mTest", json, ["mTest"]));
    }

    [Fact]
    public void HandlesSingleRecordAndDelimitedValueWithoutSeparator()
    {
        var result = JsonNode.Parse(
            Translator.Translate(
                "mTest",
                """
                {"ml_description":"説明","text":"一行\n二行"}
                """,
                ["mTest"]
            )
        );
        Assert.Equal("说明", result["ml_description"].GetValue<string>());
        Assert.Equal("第一行\n\"第二行\"\\<br>", result["text"].GetValue<string>());
    }

    [Fact]
    public void InvalidJsonIsReportedToTheHookForFallback()
    {
        Assert.ThrowsAny<JsonException>(() => Translator.Translate("mTest", "{", ["mTest"]));
    }

    [Fact]
    public void TablesWithoutEffectiveTranslationsSkipJsonParsing()
    {
        var translator = new MasterDataTranslator(
            new()
            {
                ["mEmpty"] = new()
                {
                    ["name"] = new() { ["blank"] = "", ["same"] = "same" },
                },
            }
        );
        const string untouched = "not JSON";
        Assert.Same(untouched, translator.Translate("mEmpty", untouched, ["*"]));
    }

    [Fact]
    public void CompiledRulesAreIsolatedFromSourceDictionaryChanges()
    {
        var mapping = new Dictionary<string, string> { ["名前"] = "名称" };
        var source = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        {
            ["mTest"] = new() { ["name"] = mapping },
        };
        var translator = new MasterDataTranslator(source);
        mapping["名前"] = "changed later";
        source.Clear();
        var result = JsonNode.Parse(translator.Translate("mTest", """{"name":"名前"}""", ["*"]));
        Assert.Equal("名称", result["name"].GetValue<string>());
    }
}
