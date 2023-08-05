using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UnityCopilot.Models;
using UnityCopilot.Log;
using UnityCopilot.Utils;

namespace UnityCopilot.Editor
{
    public class ChatWindow : EditorWindow
    {
        // TODO: allow the ability to use all types of logs as the context. But do it in a way that I won't cloggin up the context length
        // TODO: allow the ability to use uploaded folders and files as part of the conext without clogging up the context length

        private bool debug = true;

        // enums
        public enum Tab
        {
            Chat,
            Settings,
            Logs
        }
        private Tab selectedTab = Tab.Chat;

        public enum Assistant
        {
            Chat, 
            Programmer, 
            StoryDesigner, 
            CharacterDesigner, 
            EnvironmentDesigner
        }
        private Assistant selectedAssistant = Assistant.Chat;

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


        private const string skinPath = "Assets/ChatGPTWindow/GUISkin.guiskin";
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

        // Log tab variables
        private CustomLogger log = new CustomLogger();

        /// <summary>
        /// Send the latest error message with the chat history
        /// </summary>
        private bool applyLogging = true;

 
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

            skin = AssetDatabase.LoadAssetAtPath<GUISkin>(skinPath);
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

        private void DrawChatTab()
        {
            // Select an Assistant
            selectedAssistant = (Assistant)EditorGUILayout.EnumPopup("Assistant", selectedAssistant);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginVertical();

            foreach (ChatMessage message in chatLog)
            {
                // Align Name to the left or right depending on Name
                if (message?.role == "User")
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
                if (GUILayout.Button("Send", GUILayout.ExpandWidth(false), GUILayout.Height(60)) && !string.IsNullOrEmpty(input))
                {
                    string inputCopy = input;  // Copy the input string
                    input = string.Empty;
                    SetUpMessage(inputCopy);
                }
            //GUI.enabled = true;
            GUILayout.EndHorizontal();

            // Clear All
            if (GUILayout.Button("Clear Messages"))
            {
                chatLog.Clear();
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

            if (dropbag.droppedFiles.Count > 0 && GUILayout.Button("Remove All"))
            {
                dropbag.droppedFiles.Clear();
            }

            List<UnityEngine.Object> toRemove = new List<UnityEngine.Object>();
            foreach (UnityEngine.Object file in dropbag.droppedFiles)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(file.name);

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    toRemove.Add(file);
                }

                GUILayout.EndHorizontal();
            }

            foreach (UnityEngine.Object file in toRemove)
            {
                dropbag.droppedFiles.Remove(file);
            }

            GUILayout.EndScrollView();
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
        private void RemoveMessage()
        {
            // TODO
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
                case Assistant.Chat:
                    url = APIEndpoints.ChatUrl;
                    break;
                case Assistant.Programmer:
                    url = APIEndpoints.ProgrammerUrl;
                    break;
                case Assistant.StoryDesigner:
                    url = APIEndpoints.StoryDesignerUrl;
                    break;
                case Assistant.CharacterDesigner:
                    url = APIEndpoints.CharacterDesignerUrl;
                    break;
                case Assistant.EnvironmentDesigner:
                    url = APIEndpoints.EnvironmentDesignerUrl;
                    break;
                default:
                    if (debug) Debug.LogError("Invalid endpoint selected");
                    return;
            }


            // Create a new chat message with the user's input
            var message = new ChatMessage
            {
                role = "User",
                content = content,
                name = "You"
            };
            AddMessage(message);


            // Create a ChatInputModel
            var inputModel = new ChatInputModel
            {
                userMessage = content,
                chatHistory = new List<ChatMessage>(chatLog)
            };

            // If debug mode is enabled and there are error messages, add the latest error message to the chat history
            if (applyLogging && log.errorLog.Count > 0)
            {
                var latestError = log.GetLatestError();

                var errorMessage = new ChatMessage
                {
                    role = "error",
                    content = latestError,
                    name = "Error"
                };
                inputModel.chatHistory.Add(errorMessage);

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
                            role = "scriptContent",
                            content = scriptContent,
                            name = "Script"
                        };
                        inputModel.chatHistory.Add(scriptContentMessage);
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
                        role = "context",
                        content = fileContent,
                        name = file.name
                    };

                    inputModel.chatHistory.Add(chatMessage);
                }             
            }

            // Converts the input model into json
            string jsonData = JsonUtility.ToJson(inputModel);

            string response = await APIRequest.SendAPIRequest(url, jsonData);

            var responseMessage = new ChatMessage
            {
                role = "Assistant",
                content = response,
                name = "Assistant"
            };

            if (response != null)
                AddMessage(responseMessage);
        }
    }
}
