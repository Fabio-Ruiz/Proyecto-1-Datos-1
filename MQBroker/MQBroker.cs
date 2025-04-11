// Espacios de nombres y dependencias
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MQBrokerCustom
{
    // Nodo para lista doblemente enlazada circular que es la base del diccionario
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

        // Añade clave-valor al final, solo si no existe.
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

        // Busca si una clave está presente.
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

        // Devuelve el valor asociado a una clave(lanzando error si no existe).
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

        // Elimina un nodo con clave específica.
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

        // Devuelve todas las entradas como array de tuplas.
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

    // Cola simple tipo FIFO echha con nodos basada en LinkedListQueue
    // Esto sirve para manejar los mensajes por suscriptor en orden de llegada
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

        // Agrega un nuevo elemento al final.
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

        // Retira y devuelve el primer elemento.
        public string Dequeue()
        {
            if (front == null) return null;
            var data = front.Data;
            front = front.Next;
            if (front == null) rear = null;
            return data;
        }

        // Revisa si la cola está vacía.
        public bool IsEmpty() => front == null;
    }

    // El servidor de mensajes
    public class MQBroker
    {
        // Este es el núcleo del servidor
        private readonly TcpListener listener; // Escucha conexiones entrantes

        // Cada topic (string) tiene un diccionario de suscriptores (Guid)
        // Cada suscriptor tiene su propia cola de mensajes (LinkedListQueue)
        private readonly DiccionarioPersonalizado<string, DiccionarioPersonalizado<Guid, LinkedListQueue>> topicQueues = new();

        // El constructor del broker
        public MQBroker(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
        }


        // Inicia el servidor
        public void Start()
        {
            listener.Start();
            Console.WriteLine("MQBroker escuchando...");

            while (true)
            {
                var client = listener.AcceptTcpClient(); // Acepta conexiones entrantes
                ThreadPool.QueueUserWorkItem(HandleClient, client); // Permite atender varios clientes en simultaneo
            }
        }

        //  Maneja las solicitudes del cliente
        private void HandleClient(object obj)
        {
            // Formato:    COMANDO|arg1|arg2
            // Ejemplo: "SUBSCRIBE|{guid}|topic1"
            var client = (TcpClient)obj;
            using var stream = client.GetStream();
            byte[] buffer = new byte[2048];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] parts = request.Split('|');

            // Esto evalua que se va a ejecutar según el comando recibido
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

        // Función auxiliar para imprimir con timestamp (permite ver todas las acciones reflejadas en la consola del broker)
        private void Log(string mensaje)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}");
        }

        // Verifica si el Guid es válido, registra log-in, y confirma conexión
        private string HandleConnect(string[] parts)
        {
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var appId))
                return "ERROR|INVALID_CONNECT";

            // Imprime la acción en consola
            Log($"[CONNECT-REQ] Cliente {appId} se conectó.");

            return "CONNECTED";
        }

        // Verifica si el Guid es válido, registra log-out, y confirma desconexión
        private string HandleDisconnect(string[] parts)
        {
            if (parts.Length != 2 || !Guid.TryParse(parts[1], out var appId))
                return "ERROR|INVALID_DISCONNECT";

            // Imprime la acción en consola
            Log($"[DISCONNECT] Cliente {appId} se desconectó.");

            return "DISCONNECTED";
        }

        // Verifica si existe el topic.
        // Si no, lo crea.
        //Añade al cliente(por su Guid) una cola vacía asociada al topic.
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
                
                // Imprime la acción en consola
                Log($"[SUBSCRIBE] Cliente {appId} se suscribió al topic '{topic}'");

            }
            else
            {
                // Imprime la acción en consola
                Log($"[SUBSCRIBE] Cliente {appId} ya estaba suscrito al topic '{topic}'");
                return "ALREADY_SUBSCRIBED";
            }

            return "SUBSCRIBED";
        }

        // Elimina al cliente del diccionario del topic si existe
        private string HandleUnsubscribe(string[] parts)
        {
            if (parts.Length != 3 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_UNSUBSCRIBE";

            var topic = parts[2];
            if (!topicQueues.Contiene(topic)) return "ERROR|NOT_SUBSCRIBED";

            var subs = topicQueues.Obtener(topic);
            if (!subs.Contiene(appId)) return "ERROR|NOT_SUBSCRIBED";

            subs.Eliminar(appId);
            
            // Imprime la acción en consola
            Log($"[UNSUBSCRIBE] Cliente {appId} se desuscribió del topic '{topic}'");

            return "UNSUBSCRIBED";
        }

        // Inserta el mensaje en la cola de todos los suscriptores del topic
        private string HandlePublish(string[] parts)
        {
            if (parts.Length != 4 || !Guid.TryParse(parts[1], out var appId)) return "ERROR|INVALID_PUBLISH";

            var topic = parts[2];
            var message = parts[3];

            if (!topicQueues.Contiene(topic)) return "ERROR|TOPIC_NOT_FOUND";

            var subs = topicQueues.Obtener(topic);
            foreach (var (_, cola) in subs.Elementos())
                cola.Enqueue(message);

            // Imprime la acción en consola
            Log($"[PUBLISH] Cliente {appId} publicó mensaje en topic '{topic}': {message}");


            return "PUBLISHED";
        }

        // Extrae el siguiente mensaje de la cola del suscriptor
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
            
            // Imprime la acción en consola
            Log($"[RECEIVE] Cliente {appId} recibió mensaje del topic '{topic}': {mensaje}");


            return mensaje;
        }
    }

    // Arranca el servidor en el puerto 5000 escuchando en localhost
    public class Program
    {
        public static void Main()
        {
            var broker = new MQBroker("127.0.0.1", 5000);
            broker.Start();
        }
    }
}

