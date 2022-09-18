using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using ImageGramFunctions.Messages;
using ImageGramFunctions.Options;
using ImageGramFunctions.Services;
using ImageGramFunctions.TableEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using static ImageGramFunctionsTests.Extensions.LoggerExtensions;

namespace ImageGramFunctions.Functions.SBMessages.Tests
{
    [TestClass()]
    public class ProcessCreateCommentsTests
    {
        private readonly Mock<ILogger<ProcessCreateComments>> _mockLogger = new Mock<ILogger<ProcessCreateComments>>();
        private readonly Mock<IMessageHandlerService> _mockMessageHandler = new Mock<IMessageHandlerService>();

        private readonly Mock<IAsyncCollector<ProcessCreateCommentsMessage>> _mockProcessCreateCommentsMessageCollector = new Mock<IAsyncCollector<ProcessCreateCommentsMessage>>(MockBehavior.Strict);
        private readonly Mock<IAsyncCollector<ServiceBusMessage>> _mockRetryCollector = new Mock<IAsyncCollector<ServiceBusMessage>>(MockBehavior.Strict);

        private readonly Mock<TableClient> _mockPostDataCommentsTableClient = new();
        private readonly Mock<Response> _mockTableResponse = new();

        private List<string> _logMessages = new List<string>();
        private readonly Mock<ServiceBusMessageActions> _mockServiceBusMessageActions = new Mock<ServiceBusMessageActions>();

        private ProcessCreateCommentsMessage _processCreateCommentsMessage = new ProcessCreateCommentsMessage 
        { 
            PostId = "postId",
            Comments = "test comments"
        };

        private readonly ProcessCreateComments _functionInstance;

        public ProcessCreateCommentsTests()
        {
            _mockMessageHandler.Setup(m => m.Deserialize<ProcessCreateCommentsMessage>(It.IsAny<ServiceBusReceivedMessage>())).Returns(_processCreateCommentsMessage);
            _mockMessageHandler.Setup(m => m.PrepareRetryMessage(It.IsAny<ServiceBusReceivedMessage>())).Returns<ServiceBusReceivedMessage>((o) =>
            {
                var sb = new ServiceBusMessage(new BinaryData(o.Body));
                sb.ScheduledEnqueueTime = DateTime.UtcNow.AddMinutes(2);
                return sb;
            });

            _mockProcessCreateCommentsMessageCollector.Setup(x => x.AddAsync(It.IsAny<ProcessCreateCommentsMessage>(), default)).Returns(Task.CompletedTask).Verifiable();
            _mockRetryCollector.Setup(x => x.AddAsync(It.IsAny<ServiceBusMessage>(), default)).Returns(Task.CompletedTask).Verifiable();
            _mockServiceBusMessageActions.Setup(x => x.DeadLetterMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), default, default)).Verifiable();

            _mockTableResponse.SetupGet(x => x.Status).Returns((int)HttpStatusCode.OK);
            _mockTableResponse.SetupGet(x => x.Content).Returns(BinaryData.FromString(""));
            _mockPostDataCommentsTableClient.Setup(x => x.UpsertEntityAsync(It.IsAny<PostDataEntity>(), TableUpdateMode.Merge, default))
                .Returns(Task.FromResult(_mockTableResponse.Object));

            _mockLogger = BuildMockLogger<ProcessCreateComments>(_logMessages);

            _functionInstance = new ProcessCreateComments(_mockMessageHandler.Object);
        }

        [TestMethod()]
        public async Task Run_ProcessCreateComments_Success()
        {
            //Arrange
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromObjectAsJson(JsonConvert.SerializeObject(_processCreateCommentsMessage)));
            
            //Act
            await _functionInstance.Run(message, _mockPostDataCommentsTableClient.Object, _mockRetryCollector.Object, _mockServiceBusMessageActions.Object, _mockLogger.Object);

            //Asserts
            _mockPostDataCommentsTableClient.Verify(x => x.UpsertEntityAsync(It.IsAny<PostDataCommentsEntity>(), TableUpdateMode.Merge, default), Times.Once);

            _mockMessageHandler.Verify(m => m.Deserialize<ProcessCreateCommentsMessage>(It.IsAny<ServiceBusReceivedMessage>()), Times.Once);
            _mockMessageHandler.Verify(m => m.PrepareRetryMessage(It.IsAny<ServiceBusReceivedMessage>()), Times.Never);

            _mockLogger.Verify(x => x.Log(It.IsAny<LogLevel>(),
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((_, __) => true),
                 It.IsAny<Exception>(),
                 It.Is<Func<It.IsAnyType, Exception, string>>((_, __) => true)), Times.Exactly(3));

            _mockRetryCollector.Verify(x => x.AddAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
        }

        //more tests here
    }
}