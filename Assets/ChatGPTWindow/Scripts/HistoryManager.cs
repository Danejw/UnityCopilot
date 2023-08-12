using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityCopilot.Models;
using UnityEngine;


namespace UnityCopilot
{
    public static class HistoryManager
    {
        private const string KEY_LIST_KEY = "AllKeys";

        public static void SaveChatHistory(List<ChatMessage> history, string key = null)
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
                List<string> keys = GetAllKeys();
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    SaveAllKeys(keys);
                }
            }
        }

        public static List<ChatMessage> GetChatHistory(string key, string defaultValue = "")
        {
            string jsonString = PlayerPrefs.GetString(key, defaultValue);

            return JsonConvert.DeserializeObject<List<ChatMessage>>(jsonString);
        }

        public static List<string> GetAllKeys()
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

        private static void ClearAllChatHistory()
        {
            // Retrieve the list of all keys
            List<string> allKeys = GetAllKeys();

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

        public static void RemoveChatHistoryForKey(string targetKey)
        {
            // Delete the specific key from PlayerPrefs
            PlayerPrefs.DeleteKey(targetKey);

            // Retrieve the list of all keys
            List<string> allKeys = GetAllKeys();

            // Remove the target key from the list
            allKeys.Remove(targetKey);

            // Save the updated list back to PlayerPrefs
            SaveAllKeys(allKeys);

            // (Optional) Save changes to PlayerPrefs
            PlayerPrefs.Save();
        }

        private static void SaveAllKeys(List<string> keys)
        {
            string jsonString = JsonConvert.SerializeObject(keys);

            PlayerPrefs.SetString(KEY_LIST_KEY, jsonString);
            PlayerPrefs.Save();
        }
    }
}
