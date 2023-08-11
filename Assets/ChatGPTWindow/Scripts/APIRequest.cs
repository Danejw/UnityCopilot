using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityCopilot.Models;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace UnityCopilot
{
    public static class APIRequest
    {
        [System.Serializable]
        private class TokenResponse
        {
            public string access_token;
        }

        [System.Serializable]
        public class User
        {
            public string username;
            public string email;
            public string full_name;
            public bool? disabled;
            public int credits;
        }

        [System.Serializable]
        public class UserRegistrationResponse
        {
            public string username;
            public string email;
            public string full_name;
            public int credits;
            public string access_token;
            public string token_type;
        }



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
                //Debug.Log($"Response: {webRequest.downloadHandler.text}");

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

            Debug.Log(message: "User has loggout out");
        }

        private static async Task<T> SendWebRequest<T>(string url, string method, WWWForm formData = null, string json = null)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, method);
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

            TokenResponse tokenResponse = await SendWebRequest<TokenResponse>($"{BASE_URL}/login", "POST", formData);

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.access_token))
            {
                PlayerPrefs.SetString("authToken", tokenResponse.access_token);
                PlayerPrefs.Save();
                Debug.Log("Welcome " + username + ", you have logged in successfully!");
                return true;
            }
            else
            {
                Debug.Log("Login failed: Token not received or invalid");
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

            return await SendWebRequest<UserRegistrationResponse>($"{BASE_URL}/users/register/", "POST", null, json);
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


        private static async Task<User> SendWebRequest(string url, string method, string json)
        {
            string token = GetAuthToken();

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("No token found. Please login first.");
                return null;
            }

            UnityWebRequest webRequest = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(json))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Authorization", $"Bearer {token}");

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error with request to {url}: {webRequest.error}");
                return null;
            }
            else
            {
                User user = JsonUtility.FromJson<User>(webRequest.downloadHandler.text);
                return user;
            }
        }


        public static async Task<User> GetCurrentUser()
        {
            string url = $"{BASE_URL}/users/me/";
            return await SendWebRequest(url, "GET", null);
        }

        public static async Task<User> AddCredits(int credits)
        {
            string url = $"{BASE_URL}/users/me/credits/add/";
            string json = $"{{\"credits\": {credits}}}";
            return await SendWebRequest(url, "POST", json);
        }

        public static async Task<User> RemoveCredits(int credits)
        {
            string url = $"{BASE_URL}/users/me/credits/remove/";
            string json = $"{{\"credits\": {credits}}}";
            return await SendWebRequest(url, "POST", json);
        }

        
    }
}

