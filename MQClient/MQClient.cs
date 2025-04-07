using System;
using System.Net.Sockets;
using System.Text;

namespace MQClientGUIAppNET8
{
    public class MQClient
    {
        private string ip;
        private int port;
        private Guid appId;

        public MQClient(string ip, int port, Guid appId)
        {
            this.ip = ip;
            this.port = port;
            this.appId = appId;
        }

        private string SendRequest(string request)
        {
            using TcpClient client = new(ip, port);
            using NetworkStream stream = client.GetStream();

            byte[] data = Encoding.UTF8.GetBytes(request);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[2048];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public bool Connect()
        {
            return SendRequest($"CONNECT|{appId}") == "CONNECTED";
        }

        public bool Disconnect()
        {
            return SendRequest($"DISCONNECT|{appId}") == "DISCONNECTED";
        }


        public bool Subscribe(Topic topic) =>
            SendRequest($"SUBSCRIBE|{appId}|{topic}") == "SUBSCRIBED";

        public bool Unsubscribe(Topic topic) =>
            SendRequest($"UNSUBSCRIBE|{appId}|{topic}") == "UNSUBSCRIBED";

        public bool Publish(Message message, Topic topic) =>
            SendRequest($"PUBLISH|{appId}|{topic}|{message}") == "PUBLISHED";

        public Message Receive(Topic topic)
        {
            var response = SendRequest($"RECEIVE|{appId}|{topic}");
            if (response.StartsWith("ERROR"))
                throw new Exception(response);
            return new Message(response);
        }
    }
}