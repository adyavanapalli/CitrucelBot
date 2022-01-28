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
        /// The Cloud Tasks queue tasks will be created and run in. In the form
        /// `projects/PROJECT_ID/locations/LOCATION_ID/queues/QUEUE_ID`.
        /// </summary>
        private readonly string _queueId;

        /// <summary>
        /// The client used to create Cloud Tasks queue tasks.
        /// </summary>
        private readonly CloudTasksClient _cloudTasksClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Function()
        {
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
                ?? throw new ConfigurationErrorsException("Missing BOT_TOKEN environment variable.");

            _chatId = Environment.GetEnvironmentVariable("CHAT_ID")
                ?? throw new ConfigurationErrorsException("Missing CHAT_ID environment variable.");

            _queueId = Environment.GetEnvironmentVariable("QUEUE_ID")
                ?? throw new ConfigurationErrorsException("Missing QUEUE_ID environment variable.");

            _cloudTasksClient = CloudTasksClient.Create();
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

            await _cloudTasksClient.CreateTaskAsync(new CreateTaskRequest
            {
                Parent = _queueId,
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
