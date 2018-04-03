using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTClient
{
    public class MqttClientService
    {
        #region Field&Attribute
        private static MqttClientService _instance = null;
        private static readonly object lockObject = new object();

        public string IpAddress { get; set; }

        public int? Port { get; set; }
        public string ClientId { get; private set; }
        public bool IsConnected { get; private set; }

        private MqttQualityOfServiceLevel MqttQualityOfServiceLevel { get; set; }
        public ProtocolType ProtocolType { get; set; }
        public MqttClient mqttClient { get; set; }
        #endregion

        #region ctor
        public MqttClientService(string ipAddress, int? port = null)
        {
            if (mqttClient == null)
            {
                try
                {
                    InitOptions(ProtocolType.TCP, ipAddress, port, MqttQualityOfServiceLevel.AtMostOnce);

                    ClientId = Guid.NewGuid().ToString();
                    mqttClient = new MqttFactory().CreateMqttClient() as MqttClient;
                    mqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived; ;
                    mqttClient.Connected += MqttClient_Connected; ;
                    mqttClient.Disconnected += MqttClient_Disconnected; ;
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
       

        public static MqttClientService CreateInstance(string ipAddress)
        {
            if (_instance == null)
            {
                lock (lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new MqttClientService(ipAddress);
                    }
                }
            }
            return _instance;
        }
        #endregion

        #region Functions

        public void InitOptions(ProtocolType protocolType, string ipAddress, int? port = null, MqttQualityOfServiceLevel mqttQuality = MqttQualityOfServiceLevel.AtMostOnce)
        {
            IsConnected = false;
            IpAddress = ipAddress;
            Port = port;
            ProtocolType = protocolType;
            MqttQualityOfServiceLevel = mqttQuality;
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect()
        {
            try
            {
                mqttClient.ConnectAsync(CreateOptions());
                IsConnected = true;
            }
            catch (MqttCommunicationException ee)
            {

                throw ee;
            }

        }
        /// <summary>
        /// 断开
        /// </summary>
        public void Disconnect()
        {
            try
            {
                mqttClient.DisconnectAsync();
                IsConnected = false;
            }
            catch (MqttCommunicationException ee)
            {
                throw ee;
            }

        }
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public async void PublishMessage(string topic, string message)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            if (IsConnected)
            {
                var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel)
                .WithRetainFlag(true)//保持标志（Retain-Flag）该标志确定代理是否持久保存某个特定主题的消息。订阅该主题的新客户端将在订阅后立即收到该主题的最后保留消息。
                .Build();

                await mqttClient.PublishAsync(applicationMessage);
            }
        }
        /// <summary>
        /// 订阅主题
        /// </summary>
        /// <param name="topic"></param>
        public async void SubscribeMessage(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            if (IsConnected)
            {
                await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel).Build());
            }
          
        }
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="topic"></param>
        public async void UnsubscribeMessage(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            await mqttClient.UnsubscribeAsync(topic);
        }

        private MqttClientOptions CreateOptions()
        {
            try
            {
                //启用TLS
                var tlsOptions = new MqttClientTlsOptions
                {
                    UseTls = true,
                    IgnoreCertificateChainErrors = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                };
                var options = new MqttClientOptions
                {
                    ClientId = ClientId,
                };
                switch (ProtocolType)
                {
                    case ProtocolType.TCP:
                        options.ChannelOptions = new MqttClientTcpOptions
                        {
                            Server = IpAddress,
                            //TlsOptions = tlsOptions
                        };
                        break;
                    case ProtocolType.WS:
                        options.ChannelOptions = new MqttClientWebSocketOptions
                        {
                            Uri = IpAddress,
                            TlsOptions = tlsOptions
                        };
                        break;
                    default:
                        break;
                }
                if (options.ChannelOptions == null)
                {
                    throw new InvalidOperationException();
                }
                //设定证书
                options.Credentials = new MqttClientCredentials
                {
                    Username = "USER",
                    Password = "PASS"
                };

                options.CleanSession = true;//会话清除
                options.KeepAlivePeriod = TimeSpan.FromSeconds(10);

                //==遗言
                //WillMessage = new MqttApplicationMessage()
                //{
                //    Topic = txt_topic.Text.Trim(),
                //    Payload = (Encoding.UTF8.GetBytes("我的遗言")),
                //    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                //    Retain = false

                //},
                //ProtocolVersion = MQTTnet.Serializer.MqttProtocolVersion.V311,
                return options;
            }
            catch (Exception)
            {

                throw;
            }

        }

        #endregion

        #region Events
        public event EventHandler<MqttConnectNotifyEventArgs> OnMqttConnectNotify;
        public event EventHandler<MqttMessageNotifyEventArgs> OnMqttMessageNotify;

        private void MqttClient_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            if (OnMqttConnectNotify != null)
            {
                OnMqttConnectNotify(sender, new MqttConnectNotifyEventArgs(false));
            }
        }

        private void MqttClient_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            if (OnMqttConnectNotify != null)
            {

                OnMqttConnectNotify(sender, new MqttConnectNotifyEventArgs(true));
            }

        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (OnMqttMessageNotify != null)
            {
                OnMqttMessageNotify(sender, new MqttMessageNotifyEventArgs(true, e.ClientId, e.ApplicationMessage));
            }
        }
        #endregion




    }
}
