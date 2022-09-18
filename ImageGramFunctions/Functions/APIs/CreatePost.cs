using ImageGramFunctions.Messages;
using ImageGramFunctions.Models.Requests;
using ImageGramFunctions.Services;
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
    public class CreatePost
    {
        private const string FN_NAME = nameof(CreatePost);
        private const string SB_MESSAGE = "ProcessCreatePostMessage";
        private const string SB_CONNECTION = "AzureServiceBusConnection";

        private readonly IAzureBlobService _azureBlobService;

        public CreatePost(IAzureBlobService azureBlobService)
        {
            _azureBlobService = azureBlobService;            
        }

        [FunctionName(FN_NAME)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] UploadImageRequest uploadImageRequest,
            [ServiceBus(SB_MESSAGE, Connection = SB_CONNECTION)] IAsyncCollector<ProcessCreatePostMessage> queueMessage,
            ILogger logger)
        {
            try
            {
                logger.LogInformation($"{FN_NAME} HTTP trigger function processed a request stated.");

                if (!_azureBlobService.IsImage(uploadImageRequest.ImageFile))
                {
                    logger.LogError($"");
                    return new StatusCodeResult(StatusCodes.Status400BadRequest);
                }

                //send service bus
                var message = new ProcessCreatePostMessage { ImageCaption = uploadImageRequest.ImageCaption, ImageFile = uploadImageRequest.ImageFile };
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
