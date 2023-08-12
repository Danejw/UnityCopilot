using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityCopilot.Models;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace UnityCopilot
{
    public static partial class APIRequest
    {
        private static bool debug = true;


        private const string BASE_URL = "http://127.0.0.1:8000";



        private static string GetAuthToken()
        {
            return PlayerPrefs.GetString("authToken", "");
        }

        
        public static async Task<string> SendRequestToPythonAPI(string url, string json, string token = null)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // If a token is provided, set the Authorization header
            if (!string.IsNullOrEmpty(token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log($"Error: {webRequest.error}");

                return null;
            }
            else
            {
                if (debug) Debug.Log($"Response: {webRequest.downloadHandler.text}");

                return webRequest.downloadHandler.text;
            }
        }


        public static void Logout()
        {
            // 1. Remove the token from PlayerPrefs
            PlayerPrefs.DeleteKey("authToken");
            PlayerPrefs.Save();

            // TODO
            // 2. Optionally, send a request to the server to invalidate the token
            // (This step requires server-side support to be effective.)

            // 3. Clear any user-specific data or reset application state
            // This step is highly specific to your application's design and needs.

            if(debug) Debug.Log(message: "User has loggout out");
        }

        private static async Task<T> SendWebRequest<T>(string url, string method, WWWForm formData = null, string json = null)
        {
            string combinedUrl = $"{BASE_URL}/{url}";

            if (debug) Debug.Log("Combined Url: " + combinedUrl);


            UnityWebRequest webRequest = new UnityWebRequest(combinedUrl, method);

            if (formData != null)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(formData.data);
                webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            else if (!string.IsNullOrEmpty(json))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            string token = GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {token}");
            }
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error with request to {url}: {webRequest.error}");
                return default(T);
            }
            else
            {
                T response = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                return response;
            }
        }
        
        public static async Task<bool> Login(string username, string password)
        {
            WWWForm formData = new WWWForm();
            formData.AddField("username", username);
            formData.AddField("password", password);

            TokenResponse tokenResponse = await SendWebRequest<TokenResponse>("login", "POST", formData);

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.access_token))
            {
                PlayerPrefs.SetString("authToken", tokenResponse.access_token);
                PlayerPrefs.Save();
                if (debug) Debug.Log("Welcome " + username + ", you have logged in successfully!");
                return true;
            }
            else
            {
                if (debug) Debug.Log("Login failed: Token not received or invalid");
                return false;
            }
        }

        public static async Task<UserRegistrationResponse> RegisterUser(string username, string email, string password, string fullName = null)
        {
            string json = $"{{\"username\":\"{username}\",\"email\":\"{email}\",\"password\":\"{password}\"";
            if (!string.IsNullOrEmpty(fullName))
            {
                json += $",\"full_name\":\"{fullName}\"";
            }
            json += "}";

            if (debug) Debug.Log(json);

            var registrationResponse = await SendWebRequest<UserRegistrationResponse>("register", "POST", null, json);

            if (registrationResponse != null && !string.IsNullOrEmpty(registrationResponse.access_token))
            {
                PlayerPrefs.SetString("authToken", registrationResponse.access_token);
                PlayerPrefs.Save();
                if (debug) Debug.Log("Welcome " + username + ", you have logged in successfully!");
                return registrationResponse;
            }
            else
            {
                if (debug) Debug.Log("Registraion failed: Token not received or invalid");
                return null;
            }
        }

        public static async Task<bool> DeleteCurrentUser()
        {
            // Instead of directly sending the request, we use the SendWebRequest function
            // Note that we don't need a specific return type for delete operations, so we use <bool>
            bool response = await SendWebRequest<bool>($"{BASE_URL}/users/me/", "DELETE");

            if (response)
            {
                // Clear the token from PlayerPrefs since the user has been deleted
                PlayerPrefs.DeleteKey("authToken");
                PlayerPrefs.Save();
            }

            return response;
        }


        public static async Task<User> GetCurrentUser()
        {
            return await SendWebRequest<User>("users/me/", "GET", null);
        }

        public static async Task<User> AddCredits(int credits)
        {
            string json = $"{{\"credits\": {credits}}}";
            return await SendWebRequest<User>("users/me/credits/add/", "POST", null, json);
        }

        public static async Task<User> RemoveCredits(int credits)
        {
            string json = $"{{\"credits\": {credits}}}";
            return await SendWebRequest<User>("users/me/credits/remove/", "POST", null, json);
        }

        public static async Task<ChatMessage> CallPromptedModel(string url, ChatHistory history)
        {
            string jsonData = JsonConvert.SerializeObject(history);

            if (debug) Debug.Log("Json: " + jsonData);

            ChatMessage response = await SendWebRequest<ChatMessage>(url, "POST", null, jsonData);

            if (debug) Debug.Log("Response: " + response);

            if (response != null)
            {
                return response;
            }
            else
            {
                ChatMessage defaultMessage = new ChatMessage()
                {
                    role = "assistant",
                    name = "Default",
                    content = "I'm sorry, I couldn't fullfill your request."
                };
                return default;
            }
        }
    }
}

