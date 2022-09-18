using Azure.Messaging.ServiceBus;
using ImageGramFunctions.Options;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;

namespace ImageGramFunctions
{
    public interface IMessageHandlerService
    {
        T Deserialize<T>(ServiceBusReceivedMessage message);

        T Deserialize<T>(ServiceBusMessage message);

        ServiceBusMessage Serialize<T>(T value);

        ServiceBusMessage PrepareRetryMessage(ServiceBusReceivedMessage currentMessage);

        /// <summary>
        /// Generalized version of the PrepareRetryMessage
        /// </summary>
        ServiceBusMessage PrepareRetryMessage(ServiceBusReceivedMessage currentMessage, TimeSpan nextAttempt, TimeSpan timeout);
    }
    /// <summary>
    /// Checks message retry status, adds overall expiration and new scheduled enqueue datetime
    /// if message needs to be deadlettered, outputs null
    /// </summary>
    public class MessageHandlerService : IMessageHandlerService
    {
        private readonly RetryConfig _retryConfig;
        public MessageHandlerService(
            IOptions<RetryConfig> retryConfig)
        {
            _retryConfig = retryConfig.Value;
        }

        public T Deserialize<T>(ServiceBusReceivedMessage message)
        {
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message.Body));
        }

        public T Deserialize<T>(ServiceBusMessage message)
        {
            if (message == null) return (T)default;
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message.Body));
        }

        public ServiceBusMessage Serialize<T>(T value)
        {
            return new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
        }

        public ServiceBusMessage PrepareRetryMessage(ServiceBusReceivedMessage currentMessage)
        {
            return PrepareRetryMessage(currentMessage, TimeSpan.FromSeconds(_retryConfig.ScheduledEnqueueTime), TimeSpan.FromSeconds(_retryConfig.MessageExpiration));
        }

        public ServiceBusMessage PrepareRetryMessage(ServiceBusReceivedMessage currentMessage, TimeSpan nextAttempt, TimeSpan timeout)
        {
            var newMessage = new ServiceBusMessage(currentMessage.Body);

            if (currentMessage.ApplicationProperties.TryGetValue("ExpirationDateTime", out object expiration))
            {
                newMessage.ApplicationProperties.Add("ExpirationDateTime", expiration);
                var expirationDateTime = new DateTime(long.Parse(expiration.ToString()));
                if (expirationDateTime < DateTime.UtcNow)
                {
                    return null;
                }
            }
            else
            {
                newMessage.ApplicationProperties.Add("ExpirationDateTime", DateTime.UtcNow.AddSeconds(timeout.TotalSeconds).Ticks);
            }

            newMessage.ScheduledEnqueueTime = DateTime.UtcNow.AddSeconds(nextAttempt.TotalSeconds);
            return newMessage;
        }
    }
}
