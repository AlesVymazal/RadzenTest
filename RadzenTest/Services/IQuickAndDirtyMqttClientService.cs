
using MQTTnet.Client;

namespace BlazorDemoMqtt.Services
{
    public interface IQuickAndDirtyMqttClientService
    {
        //event Func<string, Task> MessageReceivedAsync;
        Task SubscribeAsync(string topic);
        Task UnsubscribeAsync(string topic);

        IMqttClient MqttClient { get; }
    }
}