using ImageGramFunctions.Messages;
using ImageGramFunctions.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace ImageGramFunctions.Functions.APIs
{
    public class CreateComments
    {
        private const string FN_NAME = nameof(CreatePost);
        private const string SB_MESSAGE = "ProcessCreateCommentsMessage";
        private const string SB_CONNECTION = "AzureServiceBusConnection";

        [FunctionName("CreateComments")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] CreateCommentsRequest req,
        [ServiceBus(SB_MESSAGE, Connection = SB_CONNECTION)] IAsyncCollector<ProcessCreateCommentsMessage> queueMessage,
        ILogger logger)
        {
            try
            {
                logger.LogInformation($"{FN_NAME} HTTP trigger function processed a request stated.");

                //send service bus (for simplicity, no need automapper here)
                var message = new ProcessCreateCommentsMessage { PostId = req.PostId, Comments = req.Comments };
                await queueMessage.AddAsync(message);

                logger.LogInformation($"{FN_NAME} {SB_MESSAGE} is being queued. message: {JsonConvert.SerializeObject(message)}");

                return new StatusCodeResult(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
