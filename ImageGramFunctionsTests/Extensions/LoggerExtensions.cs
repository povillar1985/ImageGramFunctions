using Microsoft.Extensions.Logging;
using Moq;
using System.Collections;

namespace ImageGramFunctionsTests.Extensions
{
    public static class LoggerExtensions
    {
        public static Mock<ILogger<T>> BuildMockLogger<T>(IList<string> logMessages)
        {
            if (logMessages == null) { throw new ArgumentNullException(nameof(logMessages)); }

            var loggerMock = new Mock<ILogger<T>>(MockBehavior.Strict);

            loggerMock
                .Setup(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true), It.IsAny<Exception>(), It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)))
                .Callback(new InvocationAction(invocation =>
                {
                    string message = string.Empty;
                    var callbackKV = ((IEnumerable)invocation.Arguments[2]) as IReadOnlyCollection<KeyValuePair<string, object>>;
                    var logMessage = callbackKV?.FirstOrDefault().Value.ToString();
                    logMessages.Add(logMessage);
                }));
            return loggerMock;
        }
    }
}
