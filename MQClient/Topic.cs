// Espacios de nombres y dependencias
using System;

namespace MQClientGUIApp
{
    // Clase que representa un canal o categoría de mensajes
    public class Topic
    {
        public string Name { get; } // Guarda el nombre del tema

        // Constructor del tema
        public Topic(string name)
        {
            // Valida que el nombre del topic no sea nulo ni vacío, para que todos los topics sean válidos
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Topic name cannot be empty.");
            Name = name;
        }

        // Para que cuando se imprima el objeto Topic, se muestre directamente su contenido
        public override string ToString() => Name;
    }
}