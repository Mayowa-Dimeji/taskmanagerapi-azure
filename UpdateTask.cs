using System;
using System.IO;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using azurebackend.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace azurebackend
{
    public class UpdateTask
    {
        private readonly ILogger<UpdateTask> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public UpdateTask(ILogger<UpdateTask> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "Tasks");
        }

        [Function("UpdateTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tasks/{taskId}")] HttpRequestData req,
            string taskId)
        {
            _logger.LogInformation("UpdateTask function triggered");

            // Get Authorization header
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                var unauthorized = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await unauthorized.WriteStringAsync("Missing or invalid token.");
                return unauthorized;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var userEmail = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                var unauthorized = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await unauthorized.WriteStringAsync("Invalid token.");
                return unauthorized;
            }

            // Get existing task
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", taskId);

            using var iterator = _container.GetItemQueryIterator<TaskModel>(query);
            var existingTask = (await iterator.ReadNextAsync()).Resource.FirstOrDefault();

            if (existingTask == null || existingTask.UserEmail != userEmail)
            {
                var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Task not found or access denied.");
                return notFound;
            }

            // Read update fields from request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var updates = JObject.Parse(body); // Safe for partial updates

            var patchOperations = new List<PatchOperation>();

            if (updates.ContainsKey("title"))
                patchOperations.Add(PatchOperation.Replace("/title", updates["title"]!.ToString()));

            if (updates.ContainsKey("description"))
                patchOperations.Add(PatchOperation.Replace("/description", updates["description"]!.ToString()));

            if (updates.ContainsKey("isCompleted"))
                patchOperations.Add(PatchOperation.Replace("/isCompleted", updates["isCompleted"]!.ToObject<bool>()));

            if (patchOperations.Count == 0)
            {
                var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("No valid fields provided for update.");
                return badRequest;
            }

            // Apply patch
            var result = await _container.PatchItemAsync<TaskModel>(
                taskId,
                new PartitionKey(userEmail),
                patchOperations
            );

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync(JsonConvert.SerializeObject(result.Resource));
            return response;
        }
    }
}
