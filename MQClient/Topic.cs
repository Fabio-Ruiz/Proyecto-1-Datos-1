using System;

namespace MQClientGUIAppNET8
{
    public class Topic
    {
        public string Name { get; }

        public Topic(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Topic name cannot be empty.");
            Name = name;
        }

        public override string ToString() => Name;
    }
}