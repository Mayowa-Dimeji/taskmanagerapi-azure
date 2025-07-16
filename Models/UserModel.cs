using Newtonsoft.Json;

namespace azurebackend.Models
{
    public class UserModel
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("password")]
        public string? Password { get; set; }

        [JsonProperty("username")]
        public string? Username { get; set; }
    }
}
