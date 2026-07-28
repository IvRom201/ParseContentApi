using System.Text.Json.Serialization;

namespace ParseContentApi.Contracts;

[JsonConverter(typeof(ContentFormatJsonConverter))]
public enum ContentFormat
{
    Unknown = 0,
    Csv = 1,
    InternalJson = 2
}