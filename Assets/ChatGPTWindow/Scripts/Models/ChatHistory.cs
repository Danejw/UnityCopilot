using System.Collections.Generic;

namespace UnityCopilot.Models
{
    [System.Serializable]
    public class ChatHistory
    {
        public List<ChatMessage> history { get; set; }
    }
}
