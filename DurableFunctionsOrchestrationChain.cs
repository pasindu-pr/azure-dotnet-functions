using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public static class DurableFunctionsOrchestrationChain
    {
        [FunctionName("DurableFunctionsOrchestrationChain")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Get input list of cities; fall back to defaults when null/empty
            var cities = context.GetInput<List<string>>()
                ?? new List<string> { "Tokyo", "Seattle", "London" };

            // Fan-out: start activity tasks in parallel
            var tasks = cities.Select(city =>
                context.CallActivityAsync<string>(nameof(ProcessCity), city)).ToArray();

            // Fan-in: wait for all results
            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }

        [FunctionName(nameof(ProcessCity))]
        public static string ProcessCity([ActivityTrigger] string city, ILogger log)
        {
            log.LogInformation("Processing city: {city}", city);
            // Minimal simulated work; replace with real API call if needed
            return $"Processed {city}";
        }

        [FunctionName("DurableFunctionsOrchestrationChain_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Read optional JSON array from request body: ["CityA","CityB"]
            string requestBody = await req.Content.ReadAsStringAsync();
            List<string> cities = null;
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                try
                {
                    cities = JsonConvert.DeserializeObject<List<string>>(requestBody);
                }
                catch
                {
                    // ignore parse error and use defaults
                    cities = null;
                }
            }

            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationChain", cities);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}