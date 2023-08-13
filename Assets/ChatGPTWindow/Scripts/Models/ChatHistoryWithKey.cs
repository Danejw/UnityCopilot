using System;
using System.Collections.Generic;

namespace UnityCopilot.Models
{
    [Serializable]
    public class ChatHistoryWithKey
    {
        public int key;
        public List<ChatMessage> history;
    }
}
