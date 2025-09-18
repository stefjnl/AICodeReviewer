using System.Text.Json.Serialization;

namespace AICodeReviewer.Web.Models
{
    /// <summary>
    /// Model information for display in the UI
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Backend model ID (e.g., "qwen/qwen3-coder")
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the model
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Model provider (e.g., "Qwen", "Moonshot AI")
        /// </summary>
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Description of the model's capabilities
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Icon for visual representation
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "🤖";
    }
}