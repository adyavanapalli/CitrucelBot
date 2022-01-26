using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Tasks.V2;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Threading;

namespace CitrucelBot
{
    /// <summary>
    /// Implements the <see cref="ICloudEventFunction{MessagePublishedData}" /> interface.
    /// </summary>
    public class Function : ICloudEventFunction<MessagePublishedData>
    {
        /// <summary>
        /// The number of seconds from now to send the notification.
        /// </summary>
        private const int SECONDS_UNTIL_INVOCATION = 2 * 60 * 60;

        /// <summary>
        /// The Telegram Bot API token.
        /// </summary>
        private readonly string _botToken;

        /// <summary>
        /// The unique identifier for the target chat.
        /// </summary>
        private readonly string _chatId;

        /// <summary>
        /// An object representing the payload within <see cref="MessagePublishedData.Message"/>. It provides
        /// identifying details for the GCP Cloud Tasks queue to create tasks in.
        /// </summary>
        private class Payload
        {
            /// <summary>
            /// The location ID of the region the Cloud Tasks queue is in.
            /// </summary>
            public string LocationId { get; set; }

            /// <summary>
            /// The project ID of the project Cloud Tasks queue is running in.
            /// </summary>
            public string ProjectId { get; set; }

            /// <summary>
            /// The queue ID of the Cloud Tasks queue tasks will be created in.
            /// </summary>
            public string QueueId { get; set; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Function()
        {
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
                ?? throw new ConfigurationErrorsException("Missing BOT_TOKEN environment variable.");

            _chatId = Environment.GetEnvironmentVariable("CHAT_ID")
                ?? throw new ConfigurationErrorsException("Missing CHAT_ID environment variable.");
        }

        /// <summary>
        /// Asynchronously handles the specified <see cref="CloudEvent" />.
        /// </summary>
        /// <param name="cloudEvent">The original <see cref="CloudEvent" /> extracted from the request.</param>
        /// <param name="messagePublishedData">The deserialized <see cref="MessagePublishedData" /> constructed from the
        /// data.</param>
        /// <param name="cancellationToken">A cancellation token which indicates if the request is aborted.</param>
        /// <returns>A task representing the potentially-asynchronous handling of the event. If the task completes, the
        /// function is deemed to be successful.</returns>
        public async System.Threading.Tasks.Task HandleAsync(CloudEvent cloudEvent,
                                                             MessagePublishedData messagePublishedData,
                                                             CancellationToken cancellationToken)
        {
            _ = cloudEvent ?? throw new ArgumentNullException(nameof(cloudEvent));
            _ = messagePublishedData ?? throw new ArgumentNullException(nameof(messagePublishedData));

            var payload = JsonConvert.DeserializeObject<Payload>(messagePublishedData.Message.TextData);
            var parent = new QueueName(payload.ProjectId, payload.LocationId, payload.QueueId);

            var cloudTasksClient = await CloudTasksClient.CreateAsync();
            await cloudTasksClient.CreateTaskAsync(new CreateTaskRequest
            {
                Parent = parent.ToString(),
                Task = new Task
                {
                    HttpRequest = new HttpRequest
                    {
                        HttpMethod = HttpMethod.Get,
                        Url = $"https://api.telegram.org/bot{_botToken}/sendMessage" +
                              $"?chat_id={_chatId}" +
                               "&text=Time to Citrucel it up folks!",
                    },
                    ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(new Random().Next(SECONDS_UNTIL_INVOCATION))),
                }
            });
        }
    }
}
