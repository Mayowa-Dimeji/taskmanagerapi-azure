using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using azurebackend.Models;

namespace azurebackend
{
    public class RegisterUser
    {
        private readonly ILogger<RegisterUser> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public RegisterUser(ILogger<RegisterUser> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "User");
        }

        [Function("RegisterUser")]
        public async Task<HttpResponseData> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("RegisterUser function triggered");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("Request Body: " + requestBody);

                var user = JsonConvert.DeserializeObject<UserModel>(requestBody);

                if (string.IsNullOrWhiteSpace(user?.Email) ||
                    string.IsNullOrWhiteSpace(user?.Password) ||
                    string.IsNullOrWhiteSpace(user?.Username))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Email, password, and username are required.");
                    return badResponse;
                }

                // 🔍 Check if user already exists
                try
                {
                    var existingUser = await _container.ReadItemAsync<UserModel>(
                        user.Email,
                        new PartitionKey(user.Email)
                    );

                    // If found, return 409 Conflict
                    var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
                    await conflictResponse.WriteStringAsync("Email already exists.");
                    return conflictResponse;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // 👍 User does not exist, continue to create

                    user.Id = Guid.NewGuid().ToString();
                    _logger.LogInformation("User before saving: " + JsonConvert.SerializeObject(user));

                    await _container.CreateItemAsync(user, new PartitionKey(user.Email));

                    var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                    await response.WriteStringAsync("User registered successfully.");
                    return response;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration failed: {ex.Message}");
                var error = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("An error occurred while registering the user.");
                return error;
            }
        }

    }
}
