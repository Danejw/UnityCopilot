using System.Collections.Generic;
using Unity.VisualScripting;

namespace UnityCopilot.Log
{
    public class LogQueue
    {
        private const int maxQueueSize = 100;
        private readonly Queue<string> queue = new Queue<string>();

        public void Add(string message) => queue.Enqueue(message);

        public Queue<string> GetQueue() => queue;

        public string GetLatest() => queue.Count > 0 ? queue.Peek() : null;

        // Trim the log queues to a maximum size
        public void TrimLogQueue()
        {
            while (queue.Count > maxQueueSize)
            {
                queue.Dequeue();
            }
        }
    }
    
}
