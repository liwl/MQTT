using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTServer
{
    public class MqttServerService
    {
        #region Field&Attribute
        private static MqttServerService _instance = null;
        private static readonly object lockObject = new object();
        private readonly ConcurrentQueue<MqttNetLogMessage> _traceMessages = new ConcurrentQueue<MqttNetLogMessage>();
        public MqttServer mqttServer { get; set; }
        #endregion

        #region ctor
        public MqttServerService()
        {
            if (mqttServer == null)
            {
                try
                {
                    MqttNetGlobalLogger.LogMessagePublished += OnTraceMessagePublished;
                    mqttServer = new MqttFactory().CreateMqttServer() as MqttServer;
                    mqttServer.ApplicationMessageReceived += MqttServer_ApplicationMessageReceived;
                    mqttServer.ClientConnected += MqttServer_ClientConnected;
                    mqttServer.ClientDisconnected += MqttServer_ClientDisconnected;
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        public static MqttServerService CreateInstance()
        {
            if (_instance == null)
            {
                lock (lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new MqttServerService();
                    }
                }
            }
            return _instance;
        }
        #endregion

        #region Functions


        public void StartServer()
        {
            try
            {
                mqttServer.StartAsync(CreateOptions());
            }
            catch (MqttCommunicationException ee)
            {
                throw ee;
            }

        }

        public void StopServer()
        {
            try
            {
                mqttServer.StopAsync();
            }
            catch (MqttCommunicationException ee)
            {
                throw ee;
            }

        }

        public void BroadCast(string topic,string message)
        {
            try
            {
                if (string.IsNullOrEmpty(topic))
                {
                    return;
                }
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(Encoding.UTF8.GetBytes(message))
                    .WithExactlyOnceQoS()
                    .Build();

                mqttServer.PublishAsync(applicationMessage);
            }
            catch (MqttCommunicationException ee)
            {
                throw ee;
            }
        }


        private MqttServerOptions CreateOptions()
        {
            try
            {

                var options = new MqttServerOptions()
                {
                    //连接验证
                    ConnectionValidator = p =>
                    {
                        if (p.ClientId == "XYZ")
                        {
                            if (p.Username != "USER" || p.Password != "PASS")
                            {
                                p.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                                return;
                            }
                        }
                        //if (p.ClientId.Length < 10)
                        //{
                        //    p.ReturnCode = MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                        //    return;
                        //}
                        p.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                    },
                    //消息拦截器
                    ApplicationMessageInterceptor = context =>
                    {
                        if (MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, "A/B/C"))
                        {
                            byte[] news = Encoding.UTF8.GetBytes("-服务器拦截处理后的消息");
                            byte[] temp = new byte[context.ApplicationMessage.Payload.Length + news.Length];
                            Array.Copy(context.ApplicationMessage.Payload, temp, context.ApplicationMessage.Payload.Length);
                            Array.Copy(news,0, temp, context.ApplicationMessage.Payload.Length,news.Length);
                            context.ApplicationMessage.Payload = temp;
                        }
                    },
                    //订阅拦截器
                    SubscriptionInterceptor = context => {
                        if (context.TopicFilter.Topic.StartsWith("A/B/C")&&context.ClientId=="XYZ")
                        {
                            context.AcceptSubscription = false;
                            context.CloseConnection = true;
                        }
                    },
                    Storage = new RetainedMessageHandler(),
                    
                    
                };
                //options.TlsEndpointOptions.Port = 8880;
                return options;
            }
            catch (Exception)
            {

                throw;
            }

        }


        private void MqttServer_ClientDisconnected(object sender, MQTTnet.Server.MqttClientDisconnectedEventArgs e)
        {
            if (OnMqttConnectNotify != null)
            {
                OnMqttConnectNotify(sender, new MqttConnectNotifyEventArgs(false,e.Client));
            }

        }

        private void MqttServer_ClientConnected(object sender, MQTTnet.Server.MqttClientConnectedEventArgs e)
        {
            if (OnMqttConnectNotify != null)
            {

                OnMqttConnectNotify(sender, new MqttConnectNotifyEventArgs(true, e.Client));
            }


        }

        private void MqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (OnMqttMessageNotify != null)
            {
                OnMqttMessageNotify(sender, new MqttMessageNotifyEventArgs(true, e.ClientId,e.ApplicationMessage));
            }

        }
        #endregion

        #region Events
        public event EventHandler<MqttConnectNotifyEventArgs> OnMqttConnectNotify;
        public event EventHandler<MqttMessageNotifyEventArgs> OnMqttMessageNotify;
        #endregion

        private async void OnTraceMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs e)
        {
            _traceMessages.Enqueue(e.TraceMessage);
            while (_traceMessages.Count > 100)
            {
                _traceMessages.TryDequeue(out _);
            }

            var logText = new StringBuilder();
            foreach (var traceMessage in _traceMessages)
            {
                logText.AppendFormat(
                    "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [{2}] [{3}] [{4}]{5}", traceMessage.Timestamp,
                    traceMessage.Level,
                    traceMessage.Source,
                    traceMessage.ThreadId,
                    traceMessage.Message,
                    Environment.NewLine);

                if (traceMessage.Exception != null)
                {
                    logText.AppendLine(traceMessage.Exception.ToString());
                }
            }
            Debug.WriteLine(logText.ToString());
            
        }
        public class RetainedMessageHandler : IMqttServerStorage
        {
            private const string Filename = "C:\\MQTT\\RetainedMessages.json";

            public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                var directory = Path.GetDirectoryName(Filename);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(Filename, JsonConvert.SerializeObject(messages));
                return Task.FromResult(0);
            }

            public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
            {
                IList<MqttApplicationMessage> retainedMessages;
                if (File.Exists(Filename))
                {
                    var json = File.ReadAllText(Filename);
                    retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
                }
                else
                {
                    retainedMessages = new List<MqttApplicationMessage>();
                }

                return Task.FromResult(retainedMessages);
            }



        }


    }
}
