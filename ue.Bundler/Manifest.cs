// Copyright (c) 2025 Yuieii.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ue.Bundler
{
    public record Dependency(
        string Team,
        string Name,
        string Version
    )
    {
        public string ToSerializedString()
        {
            return $"{Team}-{Name}-{Version}";
        }
    }

    public class DependencyConverter : JsonConverter<Dependency>
    {
        public override Dependency? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Dependency value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToSerializedString());
        }
    }
    
    public class Manifest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }
        
        [JsonPropertyName("website_url")]
        public Uri WebsiteUrl { get; set; }

        [JsonPropertyName("dependencies")] 
        public List<Dependency> Dependencies { get; set; } = [];
    }
}