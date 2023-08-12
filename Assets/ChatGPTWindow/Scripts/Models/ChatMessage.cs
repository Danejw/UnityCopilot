using System.Collections.Generic;

namespace UnityCopilot.Models
{
    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
        public string name;
        public float cost;
    }
}
