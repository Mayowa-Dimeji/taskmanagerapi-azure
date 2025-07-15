using System;
using System.IO;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json; // ✅ switched from System.Text.Json
using azurebackend.Models;

namespace azurebackend
{
    public class LoginUser
    {
        private readonly ILogger<LoginUser> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public LoginUser(ILogger<LoginUser> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "User");
        }

        [Function("LoginUser")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("LoginUser function triggered");

            var body = await new StreamReader(req.Body).ReadToEndAsync();

            // ✅ Deserialize using Newtonsoft instead of System.Text.Json
            var loginRequest = JsonConvert.DeserializeObject<UserModel>(body);

            if (string.IsNullOrWhiteSpace(loginRequest?.Email) || string.IsNullOrWhiteSpace(loginRequest?.Password))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Email and password are required.");
                return badResponse;
            }

            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                .WithParameter("@email", loginRequest.Email);

            using var iterator = _container.GetItemQueryIterator<UserModel>(query);
            var users = await iterator.ReadNextAsync();
            var user = users.Resource.FirstOrDefault();

            if (user is null || user.Password != loginRequest.Password)
            {
                var unauthorized = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                await unauthorized.WriteStringAsync("Invalid email or password.");
                return unauthorized;
            }

            var token = GenerateJwtToken(user);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync(JsonConvert.SerializeObject(new { token }));
            return response;
        }

        private string GenerateJwtToken(UserModel user)
        {
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            if (string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.Email))
            {
                throw new Exception("User ID or Email is null. Cannot generate token.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
