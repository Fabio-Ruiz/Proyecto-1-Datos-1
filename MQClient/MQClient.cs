// Espacios de nombres y dependencias
using System;
using System.Net.Sockets;
using System.Text;

namespace MQClientGUIApp
{
    // Clase pública que representa al cliente del sistema de mensajería
    public class MQClient
    {
        private string ip;
        private int port;
        private Guid appId;

        // Constructor 
        public MQClient(string ip, int port, Guid appId)
        {
            this.ip = ip;
            this.port = port;
            this.appId = appId;
        }

        // Método privado que envía una solicitud al broker y espera una respuesta
        // Este método se reutiliza por todos los comandos del cliente
        private string SendRequest(string request)
        {
            using TcpClient client = new(ip, port); // Crea una conexión TCP hacia el broker
            using NetworkStream stream = client.GetStream(); // Obtiene el flujo de datos para enviar/recibir información

            // Codifica la solicitud (request) a bytes y la envía por el socket
            byte[] data = Encoding.UTF8.GetBytes(request);
            stream.Write(data, 0, data.Length);

            // Espera la respuesta del servidor, la decodifica y la devuelve como string
            byte[] buffer = new byte[2048];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        // Envía el comando CONNECT|{appId}
        //Si el broker responde "CONNECTED", devuelve true
        // Funciona igual para los demás comandos con sus respectivas solicitudes y respuestas
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
            if (response.StartsWith("ERROR")) // Si la respuesta comienza con "ERROR", lanza una excepción
                throw new Exception(response);
            return new Message(response); // Crea un nuevo objeto Message con el contenido recibido
        }
    }
}