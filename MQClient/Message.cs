// Espacios de nombres y dependencias
using System;

namespace MQClientGUIApp
{
    // Clase que representa el mensaje
    public class Message
    {
        public string Content { get; } // Guarda el contenido del mensaje

        // Constructor del mensaje
        public Message(string content)
        {
            // Valida que no sea nulo ni vacío, para que no se creen objetos Message inválidos
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Message content cannot be empty.");
            Content = content;
        }

        // Para que cuando se imprima el objeto Message, se muestre directamente su contenido
        public override string ToString() => Content;
    }
}