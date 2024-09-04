using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace DigitalMe.Data.ValueConverters;

/// <summary>
/// Generic JSON value converter for Entity Framework Core.
/// Provides type-safe JSON serialization/deserialization for PostgreSQL JSONB columns.
/// </summary>
/// <typeparam name="T">The type to serialize to/from JSON</typeparam>
public class JsonValueConverter<T> : ValueConverter<T, string> where T : class
{
    /// <summary>
    /// Default JSON serializer options optimized for database storage.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Creates a new JSON value converter with default options.
    /// </summary>
    public JsonValueConverter() : this(DefaultOptions) { }

    /// <summary>
    /// Creates a new JSON value converter with custom options.
    /// </summary>
    /// <param name="options">Custom JSON serializer options</param>
    public JsonValueConverter(JsonSerializerOptions options) : base(
        // Convert to database: T -> string
        value => JsonSerializer.Serialize(value, options),
        // Convert from database: string -> T
        json => string.IsNullOrEmpty(json) ? default(T)! : JsonSerializer.Deserialize<T>(json, options)!)
    { }
}

/// <summary>
/// Specialized converter for Dictionary&lt;string, object&gt; properties.
/// Optimized for metadata and key-value pair storage.
/// </summary>
public class JsonDictionaryConverter : JsonValueConverter<Dictionary<string, object>>
{
    /// <summary>
    /// JSON options optimized for dictionary serialization.
    /// </summary>
    public static readonly JsonSerializerOptions DictionaryOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonDictionaryConverter() : base(DictionaryOptions) { }
}

/// <summary>
/// Specialized converter for List&lt;string&gt; properties.
/// Optimized for arrays and collections storage.
/// </summary>
public class JsonStringListConverter : JsonValueConverter<List<string>>
{
    /// <summary>
    /// JSON options optimized for string list serialization.
    /// </summary>
    public static readonly JsonSerializerOptions ListOptions = new()
    {
        WriteIndented = false,
        AllowTrailingCommas = true
    };

    public JsonStringListConverter() : base(ListOptions) { }
}