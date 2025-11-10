using System;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient httpClient = new();

        public object FunctionHandler(object input, ILambdaContext context)
        {
            try
            {
                string inputJson = input?.ToString();
                context.Logger.LogLine($"Input: {inputJson}");

                if (string.IsNullOrWhiteSpace(inputJson))
                    return new { error = "Empty request body" };

                JObject parsed = JObject.Parse(inputJson);
                string bodyJson = parsed["body"]?.ToString() ?? inputJson;

                context.Logger.LogLine($"Body: {bodyJson}");

                dynamic payload = JsonConvert.DeserializeObject(bodyJson);
                if (payload?.issue == null)
                    return new { error = "Missing issue payload" };

                string issueUrl = payload.issue.html_url;
                context.Logger.LogLine($"Issue URL: {issueUrl}");

                string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrWhiteSpace(slackUrl))
                    return new { error = "SLACK_URL not set" };

                string message = JsonConvert.SerializeObject(new
                {
                    text = $"New Issue Created: {issueUrl}"
                });

                var content = new StringContent(message, Encoding.UTF8, "application/json");
                var result = httpClient.PostAsync(slackUrl, content).Result;
                string resultBody = result.Content.ReadAsStringAsync().Result;

                return new
                {
                    statusCode = (int)result.StatusCode,
                    body = resultBody
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.ToString());
                return new { error = ex.Message };
            }
        }
    }
}
