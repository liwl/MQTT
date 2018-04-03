using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace MQTTServer
{
    public partial class Form1 : Form
    {
        public static MqttServerService mqttServerService;
        public Form1()
        {
            InitializeComponent();
          


        }


        private void Form1_Load(object sender, EventArgs e)
        {
            mqttServerService = MqttServerService.CreateInstance();
            mqttServerService.OnMqttConnectNotify += MqttServerService_OnMqttConnectNotify;
            mqttServerService.OnMqttMessageNotify += MqttServerService_OnMqttMessageNotify;
        }

        private void MqttServerService_OnMqttMessageNotify(object sender, MqttMessageNotifyEventArgs e)
        {
            ShowMessage($"客户端[{e.ClientId}]>> 主题：{e.MqttApplicationMessage.Topic} 消息：{Encoding.UTF8.GetString(e.MqttApplicationMessage.Payload)} Qos：{e.MqttApplicationMessage.QualityOfServiceLevel} 保留：{e.MqttApplicationMessage.Retain}");
        }

        private void MqttServerService_OnMqttConnectNotify(object sender, MqttConnectNotifyEventArgs e)
        {
            ShowMessage($"客户端[{e.Client.ClientId}]>> 连接{e.IsConnect}");
        }


        private void ShowMessage(string  msg)
        {
            this.Invoke(new Action(() => {
                txtInfo.AppendText(">>"+msg+ Environment.NewLine);
            }));
        }
          

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                mqttServerService.StartServer();
                ShowMessage("服务启动成功");
            }
            catch (Exception)
            {
                ShowMessage("服务启动失败");
                throw;
            }
           
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                mqttServerService.StopServer();
                ShowMessage("服务停止成功");
            }
            catch (Exception)
            {
                ShowMessage("服务停止失败");
                throw;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtInfo.Clear();
        }

        private void btnBroadcast_Click(object sender, EventArgs e)
        {
            string topic = this.txtTopic.Text.Trim();
            string message = this.txtMessage.Text.Trim();
            mqttServerService.BroadCast(topic, message);
        }
    }
}
