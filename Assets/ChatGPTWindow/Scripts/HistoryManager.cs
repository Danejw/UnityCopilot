using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityCopilot.Models;
using UnityEngine;


namespace UnityCopilot
{
    public static class HistoryManager
    {
        // player pref saves
        private const string KEY_LIST_KEY = "AllKeys";

        public static void SaveChatHistoryPlayerPref(List<ChatMessage> history, string key = null)
        {
            if (key == null)
            {
                // Use the current DateTime as the key.
                key = "ChatHistory_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            }

            string jsonString = JsonConvert.SerializeObject(history);
            PlayerPrefs.SetString(key, jsonString);

            // Ensure the key is not the master key list before proceeding
            if (key != KEY_LIST_KEY)
            {
                // Get the existing list of keys
                List<string> keys = GetAllKeysPlayerPref();
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    SaveAllKeysPlayerPref(keys);
                }
            }
        }

        public static List<ChatMessage> GetChatHistoryPlayerPref(string key, string defaultValue = "")
        {
            string jsonString = PlayerPrefs.GetString(key, defaultValue);

            return JsonConvert.DeserializeObject<List<ChatMessage>>(jsonString);
        }

        public static List<string> GetAllKeysPlayerPref()
        {
            if (PlayerPrefs.HasKey(KEY_LIST_KEY))
            {
                string jsonString = PlayerPrefs.GetString(KEY_LIST_KEY);

                // Deserialize the jsonString to get the list of keys
                return JsonConvert.DeserializeObject<List<string>>(jsonString);
            }
            else
            {
                return new List<string>();
            }
        }

        private static void ClearAllChatHistoryPlayerPref()
        {
            // Retrieve the list of all keys
            List<string> allKeys = GetAllKeysPlayerPref();

            // Delete each key from PlayerPrefs
            foreach (string key in allKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            // Delete the master key list itself
            PlayerPrefs.DeleteKey(KEY_LIST_KEY);

            // (Optional) Save changes to PlayerPrefs
            PlayerPrefs.Save();
        }

        public static void RemoveChatHistoryForKeyPlayerPref(string targetKey)
        {
            // Delete the specific key from PlayerPrefs
            PlayerPrefs.DeleteKey(targetKey);

            // Retrieve the list of all keys
            List<string> allKeys = GetAllKeysPlayerPref();

            // Remove the target key from the list
            allKeys.Remove(targetKey);

            // Save the updated list back to PlayerPrefs
            SaveAllKeysPlayerPref(allKeys);

            // (Optional) Save changes to PlayerPrefs
            PlayerPrefs.Save();
        }

        private static void SaveAllKeysPlayerPref(List<string> keys)
        {
            string jsonString = JsonConvert.SerializeObject(keys);

            PlayerPrefs.SetString(KEY_LIST_KEY, jsonString);
            PlayerPrefs.Save();
        }

        // Databse implementation
        public static async void SaveHistoryToDatabase(int key, List<ChatMessage> history)
        {
            var chatHistory = new ChatHistoryWithKey()
            {
                key = key,
                history = history,
            };

            await APIRequest.SaveHistoryToDatabase(chatHistory);
        }

        public static async Task<List<ChatHistoryWithKey>> GetAllHistoryFromDatabse()
        {
            var response = await APIRequest.GetAllHistoryFromDatabase();
            return response;
        }

        public static async Task RemoveHistoryFromDatabase(int key)
        {
            await APIRequest.RemoveHistoryFromDatabase(key);
        }

        public static async Task<ChatHistoryWithKey> GetHistoryFromDatabase(int key)
        {
            return await APIRequest.GetHistoryFromDatabase(key);
        }

    }
}
