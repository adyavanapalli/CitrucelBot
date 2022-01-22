using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Tasks.V2;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;
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
        /// The location ID of the region this Cloud Function is running in.
        /// </summary>
        private readonly string _locationId;

        /// <summary>
        /// The project ID of the project this Cloud Function is running in.
        /// </summary>
        private readonly string _projectId;

        /// <summary>
        /// The queue ID of the Cloud Tasks queue tasks will be pushed to.
        /// </summary>
        private readonly string _queueId;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Function()
        {
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
                ?? throw new ConfigurationErrorsException("Missing BOT_TOKEN environment variable.");

            _chatId = Environment.GetEnvironmentVariable("CHAT_ID")
                ?? throw new ConfigurationErrorsException("Missing CHAT_ID environment variable.");

            _locationId = Environment.GetEnvironmentVariable("LOCATION_ID")
                ?? throw new ConfigurationErrorsException("Missing LOCATION_ID environment variable.");

            _projectId = Environment.GetEnvironmentVariable("PROJECT_ID")
                ?? throw new ConfigurationErrorsException("Missing PROJECT_ID environment variable.");

            _queueId = Environment.GetEnvironmentVariable("QUEUE_ID")
                ?? throw new ConfigurationErrorsException("Missing QUEUE_ID environment variable.");
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

            var cloudTasksClient = await CloudTasksClient.CreateAsync();
            var parent = new QueueName(_projectId, _locationId, _queueId);

            await cloudTasksClient.CreateTaskAsync(new CreateTaskRequest
            {
                Parent = parent.ToString(),
                Task = new Task
                {
                    HttpRequest = new HttpRequest
                    {
                        HttpMethod = HttpMethod.Get,
                        Url = $"https://api.telegram.org/bot{_botToken}/sendMessage?chat_id={_chatId}&text=Time to Citrucel it up folks!",
                    },
                    ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(new Random().Next(SECONDS_UNTIL_INVOCATION)))
                }
            });
        }
    }
}
