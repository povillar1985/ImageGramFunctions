using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using ImageGramFunctions.Messages;
using ImageGramFunctions.TableEntities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Functions.SBMessages
{
    public class ProcessCreateComments
    {
        private const string FN_NAME = nameof(ProcessCreateComments);
        private const string SB_MESSAGE = "ProcessCreateCommentsMessage";
        private const string SB_CONNECTION = "AzureServiceBusConnection";
        private const string STORAGE_CONNECTION = "AzureStorageConnection";

        private readonly IMessageHandlerService _messageHandlerService;

        public ProcessCreateComments(IMessageHandlerService messageHandlerService)
        {
            _messageHandlerService = messageHandlerService;
        }

        [FunctionName(FN_NAME)]
        public async Task Run([ServiceBusTrigger(SB_MESSAGE, Connection = SB_CONNECTION)] ServiceBusReceivedMessage messageReceived,
        [Table("PostDataComments", Connection = STORAGE_CONNECTION)] TableClient tableClient,
        [ServiceBus(SB_MESSAGE, Connection = SB_CONNECTION)] IAsyncCollector<ServiceBusMessage> retryCollector,
        ServiceBusMessageActions messageActions,
        ILogger logger)
        {
            logger.LogInformation($"{FN_NAME} ServiceBus queue trigger function processed started.");

            ProcessCreateCommentsMessage messageObject = new();

            try
            {
                messageObject = _messageHandlerService.Deserialize<ProcessCreateCommentsMessage>(messageReceived);

                logger.LogInformation($"{FN_NAME} message: {JsonConvert.SerializeObject(messageObject)}");

                //save to table storage
                var postDataComments = new PostDataCommentsEntity
                {
                    PartitionKey = messageObject.PostId,
                    RowKey = Guid.NewGuid().ToString("N"),
                    Comments = messageObject.Comments
                };

                await tableClient.UpsertEntityAsync(postDataComments);

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
