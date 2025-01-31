using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.AI.Vision.ImageAnalysis;
using Azure;
using System.Net;
using System.Net.Http;

namespace nblab_ai_fa
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string imageUrl = req.Query["imageUrl"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            imageUrl = imageUrl ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            string responseMessage = await AnalyzeImageAsync(imageUrl); 

            return new OkObjectResult(responseMessage);
        }


        static async Task<string> AnalyzeImageAsync(string imageUrl = null)
        {
            string endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
            string key = Environment.GetEnvironmentVariable("VISION_KEY");

            ImageAnalysisClient client = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(key));

            if(imageUrl == null)
            {
                using (HttpClient httpClient = new HttpClient()) {
                    HttpResponseMessage response = await httpClient.GetAsync("https://picsum.photos/200");
                    response.EnsureSuccessStatusCode();
                    imageUrl = response.RequestMessage.RequestUri.ToString();
                    Console.WriteLine(imageUrl);
                }
            }


            ImageAnalysisResult result = client.Analyze(
                new Uri(imageUrl),
                VisualFeatures.Caption | VisualFeatures.Read,
                new ImageAnalysisOptions { GenderNeutralCaption = true });


            Console.WriteLine("Image analysis results:");
            Console.WriteLine(" Caption:");
            Console.WriteLine($"   '{result.Caption.Text}', Confidence {result.Caption.Confidence:F4}");

            Console.WriteLine(" Read:");
            foreach (DetectedTextBlock block in result.Read.Blocks)
                foreach (DetectedTextLine line in block.Lines)
                {
                    Console.WriteLine($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
                    foreach (DetectedTextWord word in line.Words)
                    {
                        Console.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence.ToString("#.####")}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
                    }
                }

            return $"Image url used: {imageUrl} \n  Image Description: {result.Caption.Text}";
        }

    }
}
