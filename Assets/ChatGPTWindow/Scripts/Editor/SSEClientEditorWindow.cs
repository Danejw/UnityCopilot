using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Policy;
using UnityCopilot.Models;
using UnityEditor;
using UnityEngine;

namespace UnityCopilot
{
    public class SSEClientEditorWindow : EditorWindow
    {
        private SSEClient sseClient;

        private const string BASE_URL = "http://127.0.0.1:8000";



        [MenuItem("Window/SSE Client")]
        public static void ShowWindow()
        {
            GetWindow<SSEClientEditorWindow>("SSE Client");
        }



        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            sseClient.StopListening();
        }


        private async void StartProcess(string url, ChatHistory history)
        {
            var response = await APIRequest.CallPromptedModel(url, history);

                // You may need to extract the client ID or other information from the response
                // Then create a new SSE client with the correct URL including the client ID
                sseClient = new SSEClient($"{BASE_URL}/sse"); // Update URL as needed
                sseClient.SSEMessageReceived += OnSSEMessageReceived;
                sseClient.StartListening();
        }



        private void OnSSEMessageReceived(string message)
        {
            Debug.Log("SSE Message: " + message);

            // You can deserialize the received message into a ChatMessage object
            //ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(message);

            //Debug.Log(chatMessage);
        }


        private void OnGUI()
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("SendSSE"))
            {
                var chatHistory = new ChatHistory
                {
                    history = new List<ChatMessage>
                {
                    new ChatMessage { name = "User1", role = "user", content = "Hi! How can I help you today?" },
                    new ChatMessage { name = "Bot", role = "assistant", content = "Hello! I'm here to assist you." },
                    new ChatMessage { name = "User1", role = "user", content = "Great! Can you tell me about ChatHistory?" },
                    new ChatMessage { name = "Bot", role = "assistant", content = "Of course! ChatHistory is a class that contains a history of chat messages." }
                }
                };


                StartProcess("auto", chatHistory);
            }

            GUILayout.EndVertical();
        }


        // SSEClient class with EditorCoroutine
        public class SSEClient
        {
            public delegate void SSEMessageHandler(string message);
            public event SSEMessageHandler SSEMessageReceived;

            private string url;

            public SSEClient(string url)
            {
                this.url = url;
            }

            public void StartListening()
            {
                EditorCoroutine.Start(ListenForSSE());
            }

            public void StopListening()
            {
                EditorCoroutine.StopAll();
            }

            private IEnumerator<object> ListenForSSE()
            {
                using (HttpClient client = new HttpClient())
                {
                    using (Stream sseStream = client.GetStreamAsync(url).Result)
                    using (StreamReader reader = new StreamReader(sseStream))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                SSEMessageReceived?.Invoke(line);
                            }
                            yield return null;
                        }
                    }
                }
            }
        }

        // Custom EditorCoroutine implementation
        public class EditorCoroutine
        {
            public static EditorCoroutine Start(IEnumerator<object> enumerator)
            {
                EditorCoroutine coroutine = new EditorCoroutine(enumerator);
                coroutine.Start();
                return coroutine;
            }

            public static void StopAll() { /* Add logic to stop all running coroutines if needed */ }

            private readonly IEnumerator<object> enumerator;

            private EditorCoroutine(IEnumerator<object> enumerator)
            {
                this.enumerator = enumerator;
            }

            private void Start()
            {
                EditorApplication.update += Update;
            }

            public void Stop()
            {
                EditorApplication.update -= Update;
            }

            private void Update()
            {
                if (!enumerator.MoveNext())
                {
                    Stop();
                }
            }
        }
    }
}
