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

        public CreateTask(ILogger<CreateTask> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "Tasks");
        }

        [Function("CreateTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("CreateTask function triggered");

            // 1. Extract JWT from Authorization header
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders) ||
                !authHeaders.First().StartsWith("Bearer "))
            {
                var unauthRes = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await unauthRes.WriteStringAsync("Missing or invalid Authorization header.");
                return unauthRes;
            }

            var token = authHeaders.First()["Bearer ".Length..].Trim();

            // 2. Validate JWT
            var jwtHandler = new JwtSecurityTokenHandler();
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
                principal = jwtHandler.ValidateToken(token, validationParams, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError("Token validation failed: " + ex.Message);
                var errorRes = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await errorRes.WriteStringAsync("Invalid token.");
                return errorRes;
            }

            // 3. Extract email from token
            var userEmail = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                var badToken = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await badToken.WriteStringAsync("Token does not contain email.");
                return badToken;
            }

            // 4. Deserialize task from request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonConvert.DeserializeObject<TaskModel>(requestBody);

            if (task == null || string.IsNullOrWhiteSpace(task.Title) || task.UserEmail != userEmail)
            {
                var badRes = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRes.WriteStringAsync("Invalid task. Ensure 'title' is provided and userEmail matches the logged-in user.");
                return badRes;
            }

            // 5. Save task to Cosmos DB
            task.Id = Guid.NewGuid().ToString();
            await _container.CreateItemAsync(task, new PartitionKey(task.UserEmail));

            var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
            await response.WriteStringAsync(JsonConvert.SerializeObject(task));
            return response;
        }
    }
}
