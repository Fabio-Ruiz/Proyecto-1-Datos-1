using System;

namespace MQClientGUIAppNET8
{
    public class Message
    {
        public string Content { get; }

        public Message(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Message content cannot be empty.");
            Content = content;
        }

        public override string ToString() => Content;
    }
}