using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using azurebackend.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

namespace azurebackend
{
    public class QueryTasks
    {
        private readonly ILogger<QueryTasks> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public QueryTasks(ILogger<QueryTasks> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer("TaskManagerDB", "Tasks");
        }

        [Function("QueryTasks")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tasks/filter")] HttpRequestData req)
        {
            _logger.LogInformation("QueryTasks function triggered");

            var token = req.Headers.GetValues("Authorization")?.FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return await Unauthorized(req, "Missing token");

            var principal = ValidateToken(token);
            if (principal == null)
                return await Unauthorized(req, "Invalid token");

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return await BadRequest(req, "Invalid token claims");

            // Parse query params
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query ?? "");

            string? priority = queryParams["priority"];
            string? status = queryParams["status"];
            string? due = queryParams["due"]; // today / tomorrow

            // Start SQL query and build parameters
            var sqlBuilder = new StringBuilder("SELECT * FROM c WHERE c.userEmail = @userEmail");
            var parameters = new Dictionary<string, object> { { "@userEmail", email! } };

            if (!string.IsNullOrWhiteSpace(priority))
            {
                sqlBuilder.Append(" AND c.priorityLevel = @priority");
                parameters.Add("@priority", priority);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isCompleted = status.ToLower() == "completed";
                sqlBuilder.Append(" AND c.isCompleted = @isCompleted");
                parameters.Add("@isCompleted", isCompleted);
            }

            if (!string.IsNullOrWhiteSpace(due))
            {
                var today = DateTime.UtcNow.Date;
                DateTime targetDate = due.ToLower() switch
                {
                    "today" => today,
                    "tomorrow" => today.AddDays(1),
                    _ => today
                };

                sqlBuilder.Append(" AND STARTSWITH(c.dueDate, @dueDate)");
                parameters.Add("@dueDate", targetDate.ToString("yyyy-MM-dd"));
            }

            // Create QueryDefinition once with all parameters
            var query = new QueryDefinition(sqlBuilder.ToString());
            foreach (var param in parameters)
            {
                query.WithParameter(param.Key, param.Value);
            }

            var iterator = _container.GetItemQueryIterator<TaskModel>(query);
            var results = new List<TaskModel>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteStringAsync(JsonConvert.SerializeObject(results));
            return res;
        }

        private async Task<HttpResponseData> Unauthorized(HttpRequestData req, string message)
        {
            var res = req.CreateResponse(HttpStatusCode.Unauthorized);
            await res.WriteStringAsync(message);
            return res;
        }

        private async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            var res = req.CreateResponse(HttpStatusCode.BadRequest);
            await res.WriteStringAsync(message);
            return res;
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secret);

            try
            {
                return tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
