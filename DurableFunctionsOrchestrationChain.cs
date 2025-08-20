using System;
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
            var orderId = context.GetInput<string>() ?? "ORDER-12345";
            var outputs = new List<string>();

            // Function chaining: each step depends on the previous
            outputs.Add(await context.CallActivityAsync<string>(nameof(ValidatePayment), orderId));
            outputs.Add(await context.CallActivityAsync<string>(nameof(CheckInventory), orderId));
            outputs.Add(await context.CallActivityAsync<string>(nameof(SendNotification), orderId));

            return outputs;
        }

        [FunctionName(nameof(ValidatePayment))]
        public static string ValidatePayment([ActivityTrigger] string orderId, ILogger log)
        {
            log.LogInformation("Validating payment for order: {orderId}", orderId);
            // Simulate processing time
            Task.Delay(5000).Wait();
            return $"Payment validated for {orderId}";
        }

        [FunctionName(nameof(CheckInventory))]
        public static string CheckInventory([ActivityTrigger] string orderId, ILogger log)
        {
            log.LogInformation("Checking inventory for order: {orderId}", orderId);
            // Simulate processing time
            Task.Delay(5000).Wait();
            return $"Inventory checked for {orderId}";
        }

        [FunctionName(nameof(SendNotification))]
        public static string SendNotification([ActivityTrigger] string orderId, ILogger log)
        {
            log.LogInformation("Sending notification for order: {orderId}", orderId);
            // Simulate processing time
            Task.Delay(5000).Wait();
            return $"Notification sent for {orderId}";
        }

        [FunctionName("DurableFunctionsOrchestrationChain_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Get order ID from query string or use default
            string orderId = req.RequestUri.ParseQueryString()["orderId"] ?? "ORDER-12345";

            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationChain", orderId);

            log.LogInformation("Started order processing orchestration with ID = '{instanceId}' for order {orderId}.", instanceId, orderId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}