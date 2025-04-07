using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MQBrokerCustom
{
    // Nodo para lista doblemente enlazada tipo diccionario
    public class DiccionarioNodo<K, V>
    {
        public K Clave;
        public V Valor;
        public DiccionarioNodo<K, V> Siguiente;
        public DiccionarioNodo<K, V> Anterior;
    }

    // Diccionario personalizado basado en lista doblemente enlazada circular
    public class DiccionarioPersonalizado<K, V>
    {
        private DiccionarioNodo<K, V> cabeza;
        private int tamaño;

        public DiccionarioPersonalizado()
        {
            cabeza = null;
            tamaño = 0;
        }

        public void Agregar(K clave, V valor)
        {
            if (Contiene(clave)) return;

            var nuevo = new DiccionarioNodo<K, V> { Clave = clave, Valor = valor };

            if (cabeza == null)
            {
                cabeza = nuevo;
                cabeza.Siguiente = cabeza;
                cabeza.Anterior = cabeza;
            }
            else
            {
                var cola = cabeza.Anterior;
                cola.Siguiente = nuevo;
                nuevo.Anterior = cola;
                nuevo.Siguiente = cabeza;
                cabeza.Anterior = nuevo;
            }
            tamaño++;
        }

        public bool Contiene(K clave)
        {
            if (cabeza == null) return false;

            var actual = cabeza;
            do
            {
                if (actual.Clave.Equals(clave)) return true;
                actual = actual.Siguiente;
            } while (actual != cabeza);

            return false;
        }

        public V Obtener(K clave)
        {
            var actual = cabeza;
            do
            {
                if (actual.Clave.Equals(clave)) return actual.Valor;
                actual = actual.Siguiente;
            } while (actual != cabeza);

            throw new Exception("Clave no encontrada");
        }

        public void Eliminar(K clave)
        {
            if (cabeza == null) return;

            var actual = cabeza;
            do
            {
                if (actual.Clave.Equals(clave))
                {
                    if (actual == cabeza && tamaño == 1)
                    {
                        cabeza = null;
                    }
                    else
                    {
                        actual.Anterior.Siguiente = actual.Siguiente;
                        actual.Siguiente.Anterior = actual.Anterior;
                        if (actual == cabeza) cabeza = actual.Siguiente;
                    }
                    tamaño--;
                    return;
                }
                actual = actual.Siguiente;
            } while (actual != cabeza);
        }

        public (K clave, V valor)[] Elementos()
        {
            var elementos = new (K, V)[tamaño];
            var actual = cabeza;
            int i = 0;

            if (actual != null)
            {
                do
                {
                    elementos[i++] = (actual.Clave, actual.Valor);
                    actual = actual.Siguiente;
                } while (actual != cabeza);
            }

            return elementos;
        }
    }

    // Cola basada en LinkedListQueue
    public class LinkedListQueue
    {
        private class Node
        {
            public string Data;
            public Node Next;
            public Node(string data) { Data = data; }
        }

        private Node front;
        private Node rear;

        public void Enqueue(string data)
        {
            var newNode = new Node(data);
            if (rear == null)
            {
                front = rear = newNode;
            }
            else
            {
                rear.Next = newNode;
                rear = newNode;
            }
        }

        public string Dequeue()
        {
            if (front == null) return null;
            var data = front.Data;
            front = front.Next;
            if (front == null) rear = null;
            return data;
        }

        public bool IsEmpty() => front == null;
    }

    public class MQBroker
    {
        private readonly TcpListener listener;
        private readonly DiccionarioPersonalizado<string, DiccionarioPersonalizado<Guid, LinkedListQueue>> topicQueues = new();

        public MQBroker(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine("MQBroker escuchando...");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }

        private void HandleClient(object obj)
        {
            var client = (TcpClient)obj;
            using var stream = client.GetStream();
            byte[] buffer = new byte[2048];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] parts = request.Split('|');

            string response = parts[0] switch
            {
                "CONNECT" => HandleConnect(parts),
                "DISCONNECT" => HandleDisconnect(parts),
                "SUBSCRIBE" => HandleSubscribe(parts),
                "UNSUBSCRIBE" => HandleUnsubscribe(parts),
                "PUBLISH" => HandlePublish(parts),
                "RECEIVE" => HandleReceive(parts),
                _ => "ERROR|UNKNOWN_COMMAND"
            };

            byte[] resp = Encoding.UTF8.GetBytes(response);
            stream.Write(resp, 0, resp.Length);
            client.Close();
        }

        private void Log(string mensaje)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}");
        }

        private string HandleConnect(string[] parts)
        {
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var appId))
                return "ERROR|INVALID_CONNECT";

            //Console.WriteLine($"[CONNECT-REQ] Cliente {appId} solicitó conexión.");
            Log($"[CONNECT-REQ] Cliente {appId} solicitó conexión.");

            return "CONNECTED";
        }

        private string HandleDisconnect(string[] parts)
        {
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var appId))
                return "ERROR|INVALID_DISCONNECT";

            //Console.WriteLine($"[DISCONNECT] Cliente {appId} se desconectó a las {DateTime.Now:T}");
            Log($"[DISCONNECT] Cliente {appId} se desconectó.");

            return "DISCONNECTED";
        }


        private string HandleSubscribe(string[] parts)
        {
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_SUBSCRIBE";

            var topic = parts[2];

            if (!topicQueues.Contiene(topic))
                topicQueues.Agregar(topic, new DiccionarioPersonalizado<Guid, LinkedListQueue>());

            var subs = topicQueues.Obtener(topic);
            if (!subs.Contiene(appId))
            {
                subs.Agregar(appId, new LinkedListQueue());
                //Console.WriteLine($"[SUBSCRIBE] Cliente {appId} se suscribió al topic '{topic}'");
                Log($"[SUBSCRIBE] Cliente {appId} se suscribió al topic '{topic}'");

            }
            else
            {
                Console.WriteLine($"[SUBSCRIBE] Cliente {appId} ya estaba suscrito al topic '{topic}'");
                return "ALREADY_SUBSCRIBED";
            }

            return "SUBSCRIBED";
        }

        private string HandleUnsubscribe(string[] parts)
        {
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_UNSUBSCRIBE";

            var topic = parts[2];
            if (!topicQueues.Contiene(topic)) return "ERROR|NOT_SUBSCRIBED";

            var subs = topicQueues.Obtener(topic);
            if (!subs.Contiene(appId)) return "ERROR|NOT_SUBSCRIBED";

            subs.Eliminar(appId);
            //Console.WriteLine($"[UNSUBSCRIBE] Cliente {appId} se desuscribió del topic '{topic}'");
            Log($"[UNSUBSCRIBE] Cliente {appId} se desuscribió del topic '{topic}'");

            return "UNSUBSCRIBED";
        }

        private string HandlePublish(string[] parts)
        {
            if (parts.Length != 4 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_PUBLISH";

            var topic = parts[2];
            var message = parts[3];

            if (!topicQueues.Contiene(topic)) return "ERROR|TOPIC_NOT_FOUND";

            var subs = topicQueues.Obtener(topic);
            foreach (var (_, cola) in subs.Elementos())
                cola.Enqueue(message);

            //Console.WriteLine($"[PUBLISH] Cliente {appId} publicó mensaje en topic '{topic}': {message}");
            Log($"[PUBLISH] Cliente {appId} publicó mensaje en topic '{topic}': {message}");


            return "PUBLISHED";
        }

        private string HandleReceive(string[] parts)
        {
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_RECEIVE";

            var topic = parts[2];
            if (!topicQueues.Contiene(topic)) return "ERROR|NOT_SUBSCRIBED";

            var subs = topicQueues.Obtener(topic);
            if (!subs.Contiene(appId)) return "ERROR|NOT_SUBSCRIBED";

            var cola = subs.Obtener(appId);
            if (cola.IsEmpty()) return "ERROR|NO_MESSAGES";

            var mensaje = cola.Dequeue();
            //Console.WriteLine($"[RECEIVE] Cliente {appId} recibió mensaje del topic '{topic}': {mensaje}");
            Log($"[RECEIVE] Cliente {appId} recibió mensaje del topic '{topic}': {mensaje}");


            return mensaje;
        }
    }

    public class Program
    {
        public static void Main()
        {
            var broker = new MQBroker("127.0.0.1", 5000);
            broker.Start();
        }
    }
}

