
using Microsoft.VisualStudio.Utilities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;

namespace BlazorDemoMqtt.Services
{
    public class QuickAndDirtyMqttClientService : BackgroundService, IQuickAndDirtyMqttClientService
    {
        private readonly ILogger<QuickAndDirtyMqttClientService> logger;
        private readonly IConfiguration configuration;

        private Dictionary<string, int> countingTopics = new Dictionary<string, int>();
        private static readonly SemaphoreSlim _dictionaryAsyncLock = new SemaphoreSlim(1, 1);

        private IMqttClient mqttClient;

        public IMqttClient MqttClient
        {
            get { return mqttClient; }
            private set { mqttClient = value; }
        }

        public QuickAndDirtyMqttClientService(ILogger<QuickAndDirtyMqttClientService> logger, IConfiguration configuration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Create a MQTT client factory
            var factory = new MqttFactory();

            // Create a MQTT client instance
            mqttClient = factory.CreateMqttClient();
            logger.LogInformation("MqttCLient created");
        }

        public async Task StopServiceAsync(CancellationToken cancellationToken)
        {
            if (MqttClient?.IsConnected == true)
            {
                logger.LogInformation("MqttCLient handlers unregistered");
                await MqttClient.DisconnectAsync(default(MqttClientDisconnectOptions), cancellationToken);
                logger.LogInformation("MqttCLient disconnected");
            }
            else
            {
                logger.LogInformation($"MqttClient already disconnected");
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            if (MqttClient?.IsConnected == true)
            {
                await _dictionaryAsyncLock.WaitAsync();
                try
                {
                    if (countingTopics.ContainsKey(topic))
                    {
                        countingTopics[topic]++;
                    }
                    else
                    {
                        countingTopics.Add(topic, 1);
                        await MqttClient.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                        logger.LogInformation($"MqttClient subscribed {topic}");
                    }
                }
                finally
                {
                    _dictionaryAsyncLock.Release();
                }                    
            }
            else
            {
                logger.LogInformation($"MqttClient not connected");
            }            
        }

        public async Task UnsubscribeAsync(string topic)
        {            
            if (MqttClient?.IsConnected == true)
            {
                await _dictionaryAsyncLock.WaitAsync();
                try
                {
                    if (countingTopics.ContainsKey(topic))
                    {
                        if (countingTopics[topic] == 1)
                        {
                            countingTopics.Remove(topic);
                            await MqttClient.UnsubscribeAsync(topic);
                            logger.LogInformation($"MqttClient unsubscribed {topic}");
                        }
                    }
                }
                finally
                {
                    _dictionaryAsyncLock.Release();
                }                                
            }
            else
            {
                logger.LogInformation($"MqttClient not connected");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var broker = configuration.GetValue<string>("MQTT_BROKER_HOST");
            int port = configuration.GetValue<int>("MQTT_BROKER_PORT");
            string clientId = Guid.NewGuid().ToString();
            //string topic = "csharp/mqtt";
            //string username = "emqx";
            //string password = "public";

            // Create MQTT client options
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port) // MQTT broker address and port
                .WithClientId(clientId)
                .WithCleanSession()
                .Build();

            while (stoppingToken.IsCancellationRequested == false)
            {
                // Connect to MQTT broker
                try
                {
                    logger.LogInformation($"MqttCLient connecting to broker:{broker}:{port}");
                    var connectResult = await MqttClient.ConnectAsync(options, stoppingToken);
                    logger.LogInformation("MqttCLient connected");

                    if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
                    {
                        logger.LogError($"Connection failed: Broker {broker}:{port}, ResultCode {connectResult.ResultCode}");
                        //throw new Exception($"Connection failed: Broker {broker}:{port}, ResultCode {connectResult.ResultCode}");
                    }
                    else
                    {
                        await Task.Delay(Timeout.Infinite, stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            await StopServiceAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        }
    }
}
