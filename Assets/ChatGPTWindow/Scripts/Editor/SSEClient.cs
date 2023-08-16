
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Net;
using System;
using System.Collections;

namespace UnityCopilot
{
    public class SSEClient
    {
        public event Action<string> SSEMessageReceived;

        private UnityWebRequest webRequest;
        private string url;
        private MonoBehaviour coroutineRunner;

        public SSEClient(MonoBehaviour runner, string url)
        {
            this.url = url;
            this.coroutineRunner = runner;
        }

        public void StartListening(string jsonData)
        {
            coroutineRunner.StartCoroutine(ListenForSSE());
        }

        public void StopListening()
        {
            if (webRequest != null)
            {
                webRequest.Abort();
                webRequest = null;
            }
        }

        private IEnumerator ListenForSSE()
        {
            using (webRequest = new UnityWebRequest(url, "GET"))
            {
                webRequest.SetRequestHeader("Accept", "text/event-stream");
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                yield return webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    if (webRequest.downloadProgress > 0)
                    {
                        string data = webRequest.downloadHandler.text;
                        if (!string.IsNullOrEmpty(data))
                        {
                            // Process each line or handle as required for your specific SSE implementation
                            SSEMessageReceived?.Invoke(data);
                        }
                    }

                    yield return new WaitForSeconds(0.1f); // Adjust the polling rate as needed
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("SSE Error: " + webRequest.error);
                }
            }
        }
    }
}
