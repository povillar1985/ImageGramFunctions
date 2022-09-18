using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using ImageGramFunctions.Messages;
using ImageGramFunctions.Options;
using ImageGramFunctions.Services;
using ImageGramFunctions.TableEntities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Functions.SBMessages
{
    /// <summary>
    /// ProcessCreatePost
    /// Save posted data to azure table storage
    /// </summary>
    public class ProcessCreatePost
    {
        private const string FN_NAME = nameof(ProcessCreatePost);
        private const string SB_MESSAGE = "ProcessCreatePostMessage";
        private const string SB_CONNECTION = "AzureServiceBusConnection";
        private const string STORAGE_CONNECTION = "AzureStorageConnection";

        private readonly IAzureBlobService _azureBlobService;
        private readonly IMessageHandlerService _messageHandlerService;
        private readonly AzureStorageConfig _azureStorageConfig;

        public ProcessCreatePost(IAzureBlobService azureBlobService, 
            IMessageHandlerService messageHandlerService,
            IOptions<AzureStorageConfig> storageOptions)
        {
            _azureBlobService = azureBlobService;
            _messageHandlerService = messageHandlerService;
            _azureStorageConfig = storageOptions.Value;
        }

        [FunctionName(FN_NAME)]
        public async Task Run([ServiceBusTrigger(SB_MESSAGE, Connection = SB_CONNECTION)] ServiceBusReceivedMessage messageReceived,
        [Table("PostData", Connection = STORAGE_CONNECTION)] TableClient tableClient,
        [ServiceBus(SB_MESSAGE, Connection = SB_CONNECTION)] IAsyncCollector<ServiceBusMessage> retryCollector,
        ServiceBusMessageActions messageActions,
        ILogger logger)
        {
            logger.LogInformation($"{FN_NAME} ServiceBus queue trigger function processed started.");

            ProcessCreatePostMessage messageObject = new();

            try
            {
                messageObject = _messageHandlerService.Deserialize<ProcessCreatePostMessage>(messageReceived);

                logger.LogInformation($"{FN_NAME} message: {JsonConvert.SerializeObject(messageObject)}");

                //save image to blob
                string fileName = $"{Guid.NewGuid():N}{Path.GetExtension(messageObject.ImageFile.FileName)}";
                await _azureBlobService.UploadBlob(_azureStorageConfig.PostedImagesContainer, messageObject.ImageFile.OpenReadStream(), fileName);

                logger.LogInformation($"{FN_NAME} New image file is uploaded successfully with filename: {fileName}");

                //save to table storage
                var postData = new PostDataEntity {
                    PartitionKey = fileName,
                    RowKey = messageObject.Id,
                    ImageCaption = messageObject.ImageCaption
                };

                await tableClient.UpsertEntityAsync(postData);

                logger.LogInformation($"{FN_NAME} Post data is successfully saved.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                logger.LogInformation($"{FN_NAME} Preparing message for retry {JsonConvert.SerializeObject(messageObject)}");

                var retryMessage = _messageHandlerService.PrepareRetryMessage(messageReceived);
                if (retryMessage != null)
                {
                    logger.LogInformation($"{FN_NAME} Adding message to retry queue {JsonConvert.SerializeObject(_messageHandlerService.Deserialize<ProcessCreatePostMessage>(retryMessage))}");
                    await retryCollector.AddAsync(retryMessage);
                }
                else
                {
                    try
                    {
                        logger.LogInformation($"{FN_NAME} Dead lettering message {Encoding.UTF8.GetString(messageReceived.Body)}.");
                        await messageActions.DeadLetterMessageAsync(messageReceived);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e.Message);
                    }
                }
            }
        }
    }
}
