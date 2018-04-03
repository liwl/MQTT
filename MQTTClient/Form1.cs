using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MQTTClient
{
    public partial class Form1 : Form
    {
        public static MqttClientService mqttClientService;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            mqttClientService = MqttClientService.CreateInstance("127.0.0.1");
            mqttClientService.OnMqttConnectNotify += MqttServerService_OnMqttConnectNotify;
            mqttClientService.OnMqttMessageNotify += MqttServerService_OnMqttMessageNotify;
        }


        private void MqttServerService_OnMqttMessageNotify(object sender, MqttMessageNotifyEventArgs e)
        {
            ShowMessage($"客户端[{e.ClientId}]>> 主题：{e.MqttApplicationMessage.Topic} 消息：{Encoding.UTF8.GetString(e.MqttApplicationMessage.Payload)} Qos：{e.MqttApplicationMessage.QualityOfServiceLevel} 保留：{e.MqttApplicationMessage.Retain}");
        }

        private void MqttServerService_OnMqttConnectNotify(object sender, MqttConnectNotifyEventArgs e)
        {
            ShowMessage($"客户端{mqttClientService.ClientId}>> 连接{e.IsConnect}");
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                var protocolType = radioButton1.Checked ? ProtocolType.TCP : ProtocolType.WS;
                var ip = this.txtIP.Text.Trim();
                var port = this.txtPort.Text.Trim();
                var qosname = this.comboBox1.SelectedText;
                var qos = MqttQualityOfServiceLevel.AtMostOnce;
                switch (qosname)
                {
                    case "AtMostOnce":
                        qos = MqttQualityOfServiceLevel.AtMostOnce;
                        break;
                    case "AtLeastOnce":
                        qos = MqttQualityOfServiceLevel.AtLeastOnce;
                        break;
                    case "ExactlyOnce":
                        qos = MqttQualityOfServiceLevel.ExactlyOnce;
                        break;
                }
                mqttClientService.InitOptions(protocolType, ip,int.Parse(port), qos);
                mqttClientService.Connect();
                ShowMessage($"客户端{mqttClientService.ClientId}连接成功");
            }
            catch (Exception)
            {
                ShowMessage("客户端连接失败");
                throw;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                mqttClientService.Disconnect();
                mqttClientService.OnMqttConnectNotify -= MqttServerService_OnMqttConnectNotify;
                mqttClientService.OnMqttMessageNotify -= MqttServerService_OnMqttMessageNotify;
                ShowMessage($"客户端{mqttClientService.ClientId}断开成功");
            }
            catch (Exception)
            {
                ShowMessage("客户端断开失败");
                throw;
            }
        }




        private void ShowMessage(string msg)
        {
            this.Invoke(new Action(() => {
                txtInfo.AppendText(">>" + msg + Environment.NewLine);
            }));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (mqttClientService.IsConnected)
                {
                    string topic = this.txtTopic.Text.Trim();
                    string message = this.txtMessage.Text.Trim();
                    mqttClientService.PublishMessage(topic, message);
                    ShowMessage($"客户端{mqttClientService.ClientId}发送主题{topic}消息{message}");
                }
            }
            catch (Exception)
            {
                ShowMessage("客户端发送失败");
                throw;
            }
        }

        private void btnSubscribe_Click(object sender, EventArgs e)
        {
            try
            {
                if (mqttClientService.IsConnected)
                {
                    string topic = this.txtTopic.Text.Trim();
                    mqttClientService.SubscribeMessage(topic);
                    ShowMessage($"客户端{mqttClientService.ClientId}订阅成功,主题{topic}");
                }
            

            }
            catch (Exception)
            {
                ShowMessage("客户端订阅失败");
                throw;
            }
            
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.txtInfo.Clear();
        }

        private void btnUnsubscribe_Click(object sender, EventArgs e)
        {
            try
            {
                if (mqttClientService.IsConnected)
                {
                    string topic = this.txtTopic.Text.Trim();
                    mqttClientService.UnsubscribeMessage(topic);
                    ShowMessage($"客户端{mqttClientService.ClientId}取消订阅成功,主题{topic}");
                }


            }
            catch (Exception)
            {
                ShowMessage("客户端取消订阅失败");
                throw;
            }
        }
    }
}
