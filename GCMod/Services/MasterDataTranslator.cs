using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GCMod.Services;

/// <summary>按数据表和字段映射替换主数据 JSON 中的日文文本。</summary>
public sealed class MasterDataTranslator
{
    private readonly Dictionary<string, FieldRule[]> _tables = new(StringComparer.Ordinal);

    private enum FieldKind
    {
        Text,
        Array,
        Delimited,
    }

    private sealed record FieldRule(
        string Name,
        FieldKind Kind,
        Dictionary<string, string> Translations
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>翻译表层级为数据表名、字段名、原文到译文的映射。</summary>
    public MasterDataTranslator(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> tables
    )
    {
        foreach (var (tableName, fields) in tables)
        {
            if (fields == null)
                continue;
            var rules = new List<FieldRule>();
            foreach (var (key, translations) in fields)
            {
                if (translations == null)
                    continue;
                var effective = translations
                    .Where(pair => !string.IsNullOrEmpty(pair.Value) && pair.Value != pair.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                if (effective.Count == 0)
                    continue;

                // 字段名和类型只解析一次；后续每条记录直接使用编译好的规则。
                var kind =
                    key.EndsWith("[]", StringComparison.Ordinal) ? FieldKind.Array
                    : key.EndsWith('|') ? FieldKind.Delimited
                    : FieldKind.Text;
                string name =
                    kind == FieldKind.Array ? key[..^2]
                    : kind == FieldKind.Delimited ? key[..^1]
                    : key;
                rules.Add(new FieldRule(name, kind, effective));
            }
            if (rules.Count > 0)
                _tables.Add(tableName, rules.ToArray());
        }
    }

    /// <summary>星号启用全部数据表，否则按表名精确匹配；空名单不启用任何表。</summary>
    public static bool IsEnabled(string tableName, string[] enabledTables) =>
        tableName != null
        && (Array.IndexOf(enabledTables, "*") >= 0 || Array.IndexOf(enabledTables, tableName) >= 0);

    /// <summary>
    /// 仅处理启用的数据表；[] 和 | 字段只替换第一种语言，空译文和未命中项保留原值。
    /// 支持记录数组和单条记录；没有修改时返回传入的原始 JSON。
    /// </summary>
    public string Translate(string tableName, string json, string[] enabledTables)
    {
        if (!IsEnabled(tableName, enabledTables) || !_tables.TryGetValue(tableName, out var fields))
            return json;

        JsonNode root = JsonNode.Parse(json);
        bool changed = false;
        if (root is JsonArray records)
        {
            foreach (JsonNode record in records)
                if (record is JsonObject obj)
                    changed |= TranslateRecord(obj, fields);
        }
        else if (root is JsonObject record)
        {
            changed = TranslateRecord(record, fields);
        }

        return changed ? root.ToJsonString(JsonOptions) : json;
    }

    private static bool TranslateRecord(JsonObject record, FieldRule[] fields)
    {
        bool changed = false;
        foreach (var rule in fields)
        {
            bool isArray = rule.Kind == FieldKind.Array;
            JsonNode value = record[rule.Name];
            JsonArray array = isArray ? value as JsonArray : null;
            if (isArray)
                value = array is { Count: > 0 } ? array[0] : null;

            if (
                value is not JsonValue scalar
                || !scalar.TryGetValue<string>(out string text)
                || text == null
            )
                continue;

            int separator = rule.Kind == FieldKind.Delimited ? text.IndexOf('|') : -1;
            string source = separator < 0 ? text : text[..separator];
            if (!rule.Translations.TryGetValue(source, out string translation))
                continue;

            if (isArray)
                array[0] = translation;
            else
                record[rule.Name] = separator < 0 ? translation : translation + text[separator..];

            changed = true;
        }
        return changed;
    }
}
