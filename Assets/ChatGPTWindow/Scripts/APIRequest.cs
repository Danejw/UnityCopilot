using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityCopilot.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityCopilot
{
    public static class APIRequest
    {
        public static async Task<string> SendAPIRequest(string url, string jsonData)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {

                return null;
            }
            else
            {
                return webRequest.downloadHandler.text;
            }
        }
    }
}
