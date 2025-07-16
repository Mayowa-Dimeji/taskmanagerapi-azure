using Newtonsoft.Json;

namespace azurebackend.Models
{
    public class TaskModel
    {
        [JsonProperty("id")] // Cosmos DB expects lowercase "id"
        public string? Id { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set; } = false;

        [JsonProperty("userEmail")]
        public string? UserEmail { get; set; }

        [JsonProperty("priorityLevel")]
        public string? PriorityLevel { get; set; } = "low";
        [JsonProperty("tag")]
        public string? Tag { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("dueDate")]
        public DateTime? DueDate { get; set; }
    }
}
