// Copyright (c) 2025 Yuieii.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ue.Bundler
{
    public class Dependency
    {
        public Dependency(string team, string name, string version)
        {
            if (!Manifest.IsValidName(name))
            {
                throw new ArgumentException("Invalid dependency name.");
            }
            
            Team = team;
            Name = name;
            Version = version;
        }

        public string Team { get; init; }
        public string Name { get; init; }
        public string Version { get; init; }

        public void Deconstruct(out string team, out string name, out string version)
        {
            team = Team;
            name = Name;
            version = Version;
        }
        
        public string ToSerializedString() => $"{Team}-{Name}-{Version}";
    }

    public class DependencyConverter : JsonConverter<Dependency>
    {
        public override Dependency? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Not implemented because we don't need to read from dependency string for now.
            throw new NotImplementedException("Reading from dependency string is not implemented for now.");
        }

        public override void Write(Utf8JsonWriter writer, Dependency value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToSerializedString());
        }
    }
    
    public class Manifest
    {
        /// <summary>
        /// Name of the mod without spaces.
        /// </summary>
        /// <remarks>
        /// Underscores get replaced with a space for display purposes in some views on the website & mod manager.
        /// </remarks>
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        
        /// <summary>
        /// A short description of the mod.
        /// </summary>
        [JsonPropertyName("description")]
        public required string Description { get; set; }
        
        [JsonPropertyName("version_number")]
        public required string VersionNumber { get; set; }

        [JsonPropertyName("website_url")]
        public required string WebsiteUrl { get; set; } = "";

        [JsonPropertyName("dependencies")] 
        public List<Dependency> Dependencies { get; set; } = [];

        public static bool IsValidName(string name) 
            => name.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');

        public static bool IsValidDescription(string description)
            => description.Length <= 250;
    }
}