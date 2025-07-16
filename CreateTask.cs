using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using azurebackend.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace azurebackend
{
    public class CreateTask
    {
        private readonly ILogger<CreateTask> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        private static readonly string[] ValidPriorities = { "low", "medium", "high" };
        private static readonly string[] ValidTags = { "personal", "work" };

        public CreateTask(ILogger<CreateTask> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "Tasks");
        }

        [Function("CreateTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("CreateTask function triggered");

            // Get JWT token
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders) ||
                !authHeaders.First().StartsWith("Bearer "))
            {
                var res = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("Missing or invalid Authorization header.");
                return res;
            }

            var token = authHeaders.First()["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET") ?? "");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(token, validationParams, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError("Token validation failed: " + ex.Message);
                var err = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await err.WriteStringAsync("Invalid token.");
                return err;
            }

            var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                var badToken = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await badToken.WriteStringAsync("Token does not contain email.");
                return badToken;
            }

            // Deserialize and validate task
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonConvert.DeserializeObject<TaskModel>(body);

            if (task == null || string.IsNullOrWhiteSpace(task.Title))
            {
                var badReq = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Task must have a title.");
                return badReq;
            }

            // Force userEmail to match JWT
            task.UserEmail = userEmail;

            // Validate priority and tag values
            if (!string.IsNullOrEmpty(task.PriorityLevel) && !ValidPriorities.Contains(task.PriorityLevel.ToLower()))
            {
                var badRes = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRes.WriteStringAsync("Priority level must be one of: low, medium, high.");
                return badRes;
            }

            if (!string.IsNullOrEmpty(task.Tag) && !ValidTags.Contains(task.Tag.ToLower()))
            {
                var badRes = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRes.WriteStringAsync("Tag must be either 'personal' or 'work'.");
                return badRes;
            }

            task.Id = Guid.NewGuid().ToString();
            task.CreatedAt = DateTime.UtcNow;

            await _container.CreateItemAsync(task, new PartitionKey(task.UserEmail));

            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteStringAsync(JsonConvert.SerializeObject(task));
            return response;
        }
    }
}
