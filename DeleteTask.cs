using System;
using System.IO;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using azurebackend.Models;
using Newtonsoft.Json;

namespace azurebackend
{
    public class DeleteTask
    {
        private readonly ILogger<DeleteTask> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public DeleteTask(ILogger<DeleteTask> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "Tasks");
        }

        [Function("DeleteTask")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tasks/{taskId}")] HttpRequestData req,
            string taskId)
        {
            _logger.LogInformation("DeleteTask function triggered");

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

            // Lookup task by ID
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", taskId);

            using var iterator = _container.GetItemQueryIterator<TaskModel>(query);
            var task = (await iterator.ReadNextAsync()).Resource.FirstOrDefault();

            if (task == null)
            {
                var notFound = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFound.WriteStringAsync("Task not found.");
                return notFound;
            }

            if (task.UserEmail != userEmail)
            {
                var forbidden = req.CreateResponse(System.Net.HttpStatusCode.Forbidden);
                await forbidden.WriteStringAsync("You are not allowed to delete this task.");
                return forbidden;
            }

            await _container.DeleteItemAsync<TaskModel>(taskId, new PartitionKey(userEmail));

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync($"Task '{task.Title}' deleted.");
            return response;
        }
    }
}
