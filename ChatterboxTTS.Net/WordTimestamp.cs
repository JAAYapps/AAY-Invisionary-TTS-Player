using System.Text.Json.Serialization;

namespace ChatterboxTTSNet;

public record WordTimestamp(
    [property: JsonPropertyName("Word")] string Word,
    [property: JsonPropertyName("Start")] double Start,
    [property: JsonPropertyName("End")] double End
);