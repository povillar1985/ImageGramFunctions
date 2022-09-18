using Azure.Data.Tables;
using ImageGramFunctions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImageGramFunctions.Functions.APIs
{
    public class RetrievePosts
    {
        private const string FN_NAME = nameof(RetrievePosts);
        private const string STORAGE_CONNECTION = "AzureStorageConnection";

        private readonly ILogger<RetrievePosts> _logger;

        public RetrievePosts(ILogger<RetrievePosts> logger)
        {
            _logger = logger;
        }

        [FunctionName(FN_NAME)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Table("PostData", Connection = STORAGE_CONNECTION)] TableClient postDataTable)
        {
            try
            {
                _logger.LogInformation($"{FN_NAME} HTTP trigger function processed a request started.");

                string skipCount = req.Query["skipCount"];
                string takeCount = req.Query["takeCount"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                skipCount = skipCount ?? data?.skipCount;
                takeCount = takeCount ?? data?.takeCount;
                
                var postDatas = new List<PostDataModel>();
                //TODO: use tableclient query / segmented query to have control of skip and take
                //retrieve comments on loop via partitionkey/postId and attach to parent model

                return new OkObjectResult(postDatas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
