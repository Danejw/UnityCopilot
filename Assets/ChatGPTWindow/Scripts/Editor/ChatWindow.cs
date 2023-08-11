using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityCopilot.Log;
using UnityCopilot.Utils;
using UnityCopilot.Models;
using Newtonsoft.Json;

namespace UnityCopilot.Editor
{
    public class ChatWindow : EditorWindow
    {
        // TODO: allow the ability to use all types of logs as the context. But do it in a way that I won't cloggin up the context length
        // TODO: allow the ability to use uploaded folders and files as part of the conext without clogging up the context length
        
        [System.Serializable]
        public class ChatHistory
        {
            public List<ChatMessage> history { get; set; }
        }


        private bool debug = true;

        // enums
        public enum Tab
        {
            Login,
            Chat,
            Settings,
            Logs
        }
        private Tab selectedTab = Tab.Login;

        public enum Assistant
        {
            Programmer, 
            Plot,
            CharacterDesigner, 
            EnvironmentDesigner,
            StoryDesigner
        }
        private Assistant selectedAssistant = Assistant.Programmer;

        public enum Role
        {
            user,
            assistant,
            system,
            function
        }

        public enum SettingOptions
        {
            APIEndpoints,
            Pathing,
            Appearance
        }
        private SettingOptions selectedSettingsTab = SettingOptions.APIEndpoints;

        public enum Log
        {
            Error,
            Warning,
            Exception,
            Message
        }
        private Log selectedLog = Log.Error;


        private const string SKIN_PATH = "Assets/ChatGPTWindow/GUISkin.guiskin";
        GUISkin skin;


        // Paths // TODO: put these into their own static class and reference it from there
        [SerializeField] private string scriptPath;
        [SerializeField] private string resourcesPath;
        [SerializeField] private string persistentPath;


        //Chat Tab Variables
        private string input = string.Empty;
        private Vector2 scrollPosition;
        private List<ChatMessage> chatLog = new List<ChatMessage>();
        
        // Drag and Drop Stuff
        private Vector2 scrollPositionDroppedFiles;
        private DragAndDropBag dropbag = new DragAndDropBag();

        // History Tab
        private Vector2 scrollPositionHistory;
        private bool showChatHistorys = false;

        // Log tab variables
        private CustomLogger log = new CustomLogger();

        /// <summary>
        /// Send the latest error message with the chat history
        /// </summary>
        private bool applyLogging = true;

        string username = string.Empty;
        string password = string.Empty;

 
        [MenuItem("Tools/Unity Co-Pilot")]
        public static void ShowWindow()
        {
            GetWindow<ChatWindow>("Unity Co-Pilot");
        }

        private void OnGUI()
        {
            if (skin != null) GUI.skin = skin;

            DrawMainToolbar();
        }

        private void OnEnable()
        {
            // Set up the logger tp listen for log messages
            Application.logMessageReceived += HandleLog;

            skin = AssetDatabase.LoadAssetAtPath<GUISkin>(SKIN_PATH);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }


        // GUI Draws
        #region UI Draws

        // Main Tabs
        private void DrawMainToolbar()
        {
            selectedTab = (Tab)GUILayout.Toolbar((int)selectedTab, Enum.GetNames(typeof(Tab)));

            switch (selectedTab)
            {
                case Tab.Login:
                    DrawLoginTab();
                    break;
                case Tab.Chat: // Chat tab
                    DrawChatTab();
                    break;
                case Tab.Settings: // Settings tab
                    DrawSettingsTab();
                    break;
                case Tab.Logs: // Debug logs tab
                    DrawLogTab();
                    break;
            }
        }

        private async void DrawLoginTab()
        {
            GUILayout.BeginVertical();

            username = GUILayout.TextField(username);
            password = GUILayout.TextField(password);

            
            if (GUILayout.Button("Login"))
            {
                await APIRequest.Login(username, password);
                password = string.Empty;
            }

            if (GUILayout.Button("Logout"))
            {
                APIRequest.Logout();
                username = string.Empty;
                password = string.Empty;
            }

            if (GUILayout.Button("Get Current User Data"))
            {
                var user = await APIRequest.GetCurrentUser();
                if(debug) Debug.Log(user.username);
            }


            if (GUILayout.Button("Buy Credits"))
            {
                var user = await APIRequest.AddCredits(5);
                if (debug) Debug.Log(user.credits);
            }


            if (GUILayout.Button("Spend Credits"))
            {
                var user = await APIRequest.RemoveCredits(1);
                if (debug) Debug.Log(user.credits);
            }

            GUILayout.EndVertical();
        }

        private void DrawChatTab()
        {
            GUILayout.BeginHorizontal();
                // History
                if (GUILayout.Button("Conversations"))
                {
                    if (showChatHistorys) showChatHistorys = false;
                    else showChatHistorys = true;
                }

                if (GUILayout.Button("Save Conversation"))
                {
                    HistoryManager.SaveChatHistory(chatLog);
                }
            GUILayout.EndHorizontal();

            if (showChatHistorys)
                DrawHistoryTab();

            // Select an Assistant
            selectedAssistant = (Assistant)EditorGUILayout.EnumPopup("Assistant", selectedAssistant);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginVertical();


            // Chat Log
            foreach (ChatMessage message in chatLog)
            {
                GUILayout.BeginHorizontal();
                // Align Name to the left or right depending on Name
                if (message?.role == "user")
                {
                    GUIStyle userlabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                    GUILayout.Label(message?.name, userlabelStyle);
                }
                else
                {
                    GUIStyle assistantlabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleRight
                    };
                    GUILayout.Label(message?.name, assistantlabelStyle);
                }


                if (GUILayout.Button("X", WarningButtonStyle(), GUILayout.Width(30)))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Confirm Delete",
                       $"Are you sure you want to remove that chat messsage?",
                          "Yes", "No");

                    if (confirm)
                    {
                        RemoveMessage(message);
                    }           
                }


                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(false);

                Regex codeRegex = new Regex(@"```csharp(.*?)```", RegexOptions.Singleline);
                MatchCollection matches = codeRegex.Matches(message.content);
                int start = 0;

                foreach (Match match in matches)
                {
                    // Draw the part of the message before the code
                    string beforeCode = message.content.Substring(start, match.Index - start);
                    GUILayout.TextField(beforeCode, GUILayout.ExpandWidth(true));

                    // Draw the code snippet in a box
                    string code = match.Groups[1].Value;  // Extract the code snippet
                    DrawCodeSnippet(code);

                    start = match.Index + match.Length;
                }

                // Draw the part of the message after the last code snippet
                string afterCode = message.content.Substring(start);
                GUILayout.TextField(afterCode, GUILayout.ExpandWidth(true));

                EditorGUI.EndDisabledGroup();
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Drag and Drop Files
            DrawDropArea();

            // Input
            GUILayout.BeginHorizontal();
                input = GUILayout.TextArea(input, GUILayout.ExpandWidth(true), GUILayout.Width(position.width - 57), GUILayout.Height(60));

                // Disables the send button while sending a request
                //GUI.enabled = false;
                if (GUILayout.Button("Send", GoButtonStyle(), GUILayout.ExpandWidth(false), GUILayout.Height(60)) && !string.IsNullOrEmpty(input))
                {
                    string inputCopy = input;  // Copy the input string
                    input = string.Empty;
                    SetUpMessage(inputCopy);
                }
            //GUI.enabled = true;
            GUILayout.EndHorizontal();

            // Clear All
            if (GUILayout.Button("Clear Messages", WarningButtonStyle()))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Delete",
                   $"Are you sure you want to clear all chat messages?",
                      "Yes", "No");

                if (confirm)
                {
                    chatLog.Clear();
                }
            }

            DrawDroppedFiles();
        }

        private void DrawSettingsTab()
        {
            selectedSettingsTab = (SettingOptions)GUILayout.Toolbar((int)selectedSettingsTab, Enum.GetNames(typeof(SettingOptions)));

            switch (selectedSettingsTab)
            {
                case SettingOptions.APIEndpoints:
                    DrawEndpointsTab();
                    break;
                case SettingOptions.Pathing:
                    DrawPathTab();
                    break;
                case SettingOptions.Appearance:
                    DrawAppearanceTab();
                    break;
            }
        }

        private void DrawLogTab()
        {
            selectedLog = (Log)GUILayout.Toolbar((int)selectedLog, Enum.GetNames(typeof(Log)));

            GUILayout.BeginHorizontal();
            applyLogging = GUILayout.Toggle(applyLogging, "Apply Error Log In Conext");
            GUILayout.EndHorizontal();

            switch (selectedLog)
            {
                case Log.Error:
                    DrawErrorLogs();
                    break;
                case Log.Warning:
                    DrawWarningLogs();
                    break;
                case Log.Exception:
                    DrawExceptionLogs();
                    break;
                case Log.Message:
                    DrawMessageLogs();
                    break;
            }
        }


        // History Tab
        private void DrawHistoryTab()
        {
            List<string> keys = HistoryManager.GetAllKeys();

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(key))
                    {
                        // Do something when the key is pressed, for example, fetch the value from PlayerPrefs
                        chatLog = HistoryManager.GetChatHistory(key);
                    }

                    // Delete button
                    if (GUILayout.Button("Delete", WarningButtonStyle(), GUILayout.Width(60)))  // Setting a fixed width for the delete button
                    {
                        bool confirm = EditorUtility.DisplayDialog(
                            "Confirm Delete",
                            $"Are you sure you want to delete chat history for key: {key}?",
                            "Yes", "No");

                        if (confirm)
                        {
                            HistoryManager.RemoveChatHistoryForKey(key);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No keys found!");
            }
        }


        // Settings Tabs
        private void DrawPathTab()
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);

            GUILayout.Label("To be implemented for loading folders into the context");

            GUILayout.Label("Script Path:");
            scriptPath = GUILayout.TextField(scriptPath);
            if (scriptPath == string.Empty) { scriptPath = Path.Combine(Application.dataPath, "Scripts"); }

            GUILayout.Label("Resources Path:");
            resourcesPath = GUILayout.TextField(resourcesPath);
            if (resourcesPath == string.Empty) { resourcesPath = Path.Combine(Application.dataPath, "Resources"); }

            GUILayout.Label("Persistent Path:");
            persistentPath = GUILayout.TextField(persistentPath);
            if (persistentPath == string.Empty) { persistentPath = Application.persistentDataPath; }

            GUILayout.EndVertical();
        }

        private void DrawAppearanceTab()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("GUISkin: " + (skin != null ? skin.name : "None"));

            GUILayout.EndVertical();
        }

        private void DrawEndpointsTab()
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);

            GUILayout.Label("Chat API Endpoint URL:");
            GUILayout.TextField(APIEndpoints.ChatUrl);

            GUILayout.Space(10);

            GUILayout.Label("Unity Programmer API Endpoint URL:");
            GUILayout.TextField(APIEndpoints.ProgrammerUrl);

            GUILayout.Space(10);

            GUILayout.Label("Story Designer API Endpoint URL:");
            GUILayout.TextField(APIEndpoints.StoryDesignerUrl);

            GUILayout.Space(10);

            GUILayout.Label("Character Designer API Endpoint URL:");
            GUILayout.TextField(APIEndpoints.CharacterDesignerUrl);

            GUILayout.Space(10);

            GUILayout.Label("Environment Designer API Endpoint URL:");
            GUILayout.TextField(APIEndpoints.EnvironmentDesignerUrl);

            GUILayout.Space(10);

            GUILayout.EndVertical();
        }


        // Log Tabs
        private void DrawErrorLogs()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                GUILayout.BeginVertical();

                    foreach (string log in log.errorLog)
                    {
                        GUILayout.BeginVertical("box");
                            GUILayout.Label(log);
                        GUILayout.EndVertical();
                    }

                GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawWarningLogs()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical();

            foreach (string log in log.warningLog)
            {
                GUILayout.BeginVertical("box");
                    GUILayout.Label(log);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawExceptionLogs()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical();

            foreach (string log in log.exceptionLog)
            {
                GUILayout.BeginVertical("box");
                    GUILayout.Label(log);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawMessageLogs()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical();

            foreach (string log in log.messageLog)
            {
                GUILayout.BeginVertical("box");
                    GUILayout.Label(log);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }


        // Other UI Components
        private void DrawCodeSnippet(string code)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();  // Add a flexible space before the button
            if (GUILayout.Button("Copy", GUILayout.Width(80), GUILayout.Height(20)))
            {
                EditorGUIUtility.systemCopyBuffer = code;
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(false);
            GUILayout.TextArea("<color=#ffff76>" + code + "</color>", GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }

        private void DrawDropArea()
        {
            GUILayout.Space(10);
            GUILayout.Label("Drag and Drop C# Files");
            GUILayout.Box("Drop Area", GUILayout.Height(50));
            dropbag.HandleDragAndDropEvents();
        }

        private void DrawDroppedFiles()
        {
            float scrollHeight = Mathf.Min(100, dropbag.droppedFiles.Count * 50);

            scrollPositionDroppedFiles = GUILayout.BeginScrollView(scrollPositionDroppedFiles, GUILayout.Width(position.width), GUILayout.Height(scrollHeight));

            if (dropbag.droppedFiles.Count > 0 && GUILayout.Button("Remove All", WarningButtonStyle()))
            {
                bool confirm = EditorUtility.DisplayDialog(
                "Confirm Remove All",
                            $"Are you sure you want to remove all loaded files?",
                            "Yes", "No");

                if (confirm)
                {
                    dropbag.droppedFiles.Clear();
                }         
            }

            List<UnityEngine.Object> toRemove = new List<UnityEngine.Object>();
            foreach (UnityEngine.Object file in dropbag.droppedFiles)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(file.name);

                if (GUILayout.Button("Remove", WarningButtonStyle(), GUILayout.Width(80)))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Remove All",
                   $"Are you sure you want to remove this file?",
                      "Yes", "No");

                    if (confirm)
                    {
                        toRemove.Add(file);
                    }               
                }

                GUILayout.EndHorizontal();
            }

            foreach (UnityEngine.Object file in toRemove)
            {
                dropbag.droppedFiles.Remove(file);
            }

            GUILayout.EndScrollView();
        }


        // Custom Styling
        private GUIStyle WarningButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            // Create a Texture2D and set its color to red
            Texture2D redTexture = new Texture2D(1, 1);
            redTexture.SetPixel(0, 0, new Color(.1f, .1f, .1f)); // This creates a dark red color
            redTexture.Apply();

            style.normal.background = redTexture;

            return style;
        }

        private GUIStyle GoButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            // Create a Texture2D and set its color to dark green
            Texture2D greenTexture = new Texture2D(1, 1);
            greenTexture.SetPixel(0, 0, new Color(.1f, .1f, .1f));  // This creates a dark green color
            greenTexture.Apply();

            style.normal.background = greenTexture;

            return style;
        }


        #endregion

        // Chat Log
        private void AddMessage(ChatMessage message)
        {
            chatLog.Add(message);

            Repaint();

            // Update the scroll position to be at the bottom
            scrollPosition.y = float.MaxValue;
        }
        private void RemoveMessage(ChatMessage message)
        {
            if (chatLog.Contains(message))
            {
                chatLog.Remove(message);
            }
        }


        // Logging
        private void HandleLog(string message, string stackTrace, LogType type)
        {
            log.LogFormat(type, message);
        }


        // Prompt Creation
        private async Task SetUpMessage(string content)
        {
            // Select the url based on the selected endpoint
            string url;
            switch (selectedAssistant)
            {
                case Assistant.Programmer:
                    url = APIEndpoints.ProgrammerPythonUrl;
                    break;
                case Assistant.Plot:
                    url = APIEndpoints.PlotPythonUrl;
                    break;
                case Assistant.CharacterDesigner:
                    url = APIEndpoints.CharacterPythonUrl;
                    break;
                case Assistant.EnvironmentDesigner:
                    url = APIEndpoints.EnvironmentPythonUrl;
                    break;
                case Assistant.StoryDesigner:
                    url = APIEndpoints.StoryPythonUrl;
                    break;
                default:
                    if (debug) Debug.LogError("Invalid endpoint selected");
                    return;

            }


            // Create a new chat message with the user's input
            var message = new ChatMessage
            {
                role = Role.user.ToString(),
                content = content,
                name = "You"
            };
            AddMessage(message);


            // Create a ChatInputModel
            ChatHistory chat = new ChatHistory
            {
                history = new List<ChatMessage>(chatLog)
            };

            // If debug mode is enabled and there are error messages, add the latest error message to the chat history
            if (applyLogging && log.errorLog.Count > 0)
            {
                var latestError = log.GetLatestError();

                var errorMessage = new ChatMessage
                {
                    role = Role.user.ToString(),
                    content = latestError,
                    name = "Error"
                };
                chat.history.Add(errorMessage);

                // Extract the script file path from the error message
                string scriptPath = FileUtils.ExtractScriptPathFromError(latestError);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Read the script file content and add it to the chat history
                    string scriptContent = FileUtils.ReadScriptContentFromPath(scriptPath);
                    if (!string.IsNullOrEmpty(scriptContent))
                    {
                        var scriptContentMessage = new ChatMessage
                        {
                            role = Role.user.ToString(),
                            content = scriptContent,
                            name = "Script"
                        };
                        chat.history.Add(scriptContentMessage);
                    }
                }
            }

            // If there are files loaded
            if (dropbag.droppedFiles != null)
            {
                foreach (var file in dropbag.droppedFiles)
                {
                    string fileContent = FileUtils.ReadCSharpFile(file);

                    ChatMessage chatMessage = new ChatMessage()
                    {
                        role = Role.user.ToString(),
                        content = fileContent,
                        name = file.name
                    };

                    chat.history.Add(chatMessage);
                }             
            }

            // Converts the input model into json
            string jsonData = JsonConvert.SerializeObject(chat);

            if (PlayerPrefs.HasKey("authToken"))
            {
                //ChatMessage response = await APIRequest.CallPromptedModel(url, jsonData);

                //Debug.Log(response);

                //ChatMessage responseData = JsonConvert.DeserializeObject<ChatMessage>(response);

                //if (response != null)
                // AddMessage(response);
            }
            else
            {
                if (debug) Debug.Log("User is Unauthorized and needs to sign in.");
            }
        }
    }



    public static class HistoryManager
    {
        private const string KEY_LIST_KEY = "AllKeys";

        public static void SaveChatHistory(List<ChatMessage> history, string key=null)
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
