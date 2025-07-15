using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using azurebackend.Models;
using Newtonsoft.Json;


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



            // Get a reference to the database and container (change these to match your Cosmos DB)
            _container = _cosmosClient.GetContainer("TaskManagerDB", "User"); // change names as needed
        }

        [Function("RegisterUser")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("RegisterUser function triggered");

            try
            {
                // Read the request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                _logger.LogInformation("Request Body: " + requestBody);
                var user = JsonConvert.DeserializeObject<UserModel>(requestBody);



                if (string.IsNullOrWhiteSpace(user?.Email) || string.IsNullOrWhiteSpace(user?.Password))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Email and password are required.");
                    return badResponse;
                }

                // Generate ID and store to Cosmos DB
                user.Id = Guid.NewGuid().ToString();
                _logger.LogInformation("User before saving: " + JsonConvert.SerializeObject(user));

                await _container.CreateItemAsync(user, new PartitionKey(user.Email));

                // Respond with success
                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteStringAsync("User registered successfully.");
                return response;
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