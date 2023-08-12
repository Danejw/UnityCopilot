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
    public partial class ChatWindow : EditorWindow
    {
        // TODO: allow the ability to use all types of logs as the context. But do it in a way that I won't cloggin up the context length
        // TODO: allow the ability to use uploaded folders and files as part of the conext without clogging up the context length

        private bool debug = true;

        // enums
        public enum Tab
        {
            Chat,
            Logs,
            Settings
        }
        private Tab selectedTab = Tab.Chat;

        public enum UserStatus
        {
            LoggedIn,
            LoggedOut,
        }
        private UserStatus userSatus = UserStatus.LoggedOut;

        public enum SignOptions
        {
            Login,
            Register,
        }
        private SignOptions signOption = SignOptions.Login;


        public enum Assistant
        {
            Programmer,
            PlotCreator,
            CharacterCreator,
            EnvironmentCreator,
            StoryCreator,
            StyleCreator,
            GrammarCorrection,
            KeywordExtraction,
            Summarizer,
            Critic,
            Auto
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
            Pathing,
            Appearance
        }
        private SettingOptions selectedSettingsTab = SettingOptions.Pathing;

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

        // User
        private User currentUser;

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
        private LogController log = new LogController();

        /// <summary>
        /// Send the latest error message with the chat history
        /// </summary>
        private bool applyLogging = true;

        // Login variables
        string username = string.Empty;
        string password = string.Empty;

        // Register variables
        string registerUsername = string.Empty;
        string registerEmail = string.Empty;
        string registerPassword = string.Empty;
        string registerFullName = string.Empty;



        [MenuItem("Tools/Unity Co-Pilot")]
        public static void ShowWindow()
        {
            GetWindow<ChatWindow>("Unity Co-Pilot");
        }

        private void OnGUI()
        {
            if (skin != null) GUI.skin = skin;



            DrawUserInfo();

            DrawSignInOptions();

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
            if (userSatus.Equals(UserStatus.LoggedOut)) { return; }

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


        private void DrawSignInOptions()
        {
            if (userSatus.Equals(UserStatus.LoggedIn)) { return; }

            signOption = (SignOptions)GUILayout.Toolbar((int)signOption, Enum.GetNames(typeof(SignOptions)));

            switch (signOption)
            {
                case SignOptions.Login:
                    DrawLoginForm();
                    break;
                case SignOptions.Register:
                    DrawRegistrationForm();
                    break;
            }
        }

        private async void DrawLoginForm()
        {
            // Calculate the centered position for the login box
            float boxWidth = 300; // or whatever width you want
            float boxHeight = 300; // or whatever height you want
            Rect centeredRect = new Rect((Screen.width - boxWidth) / 2, (Screen.height - boxHeight) / 2, boxWidth, boxHeight);

            // Define custom box style with padding
            GUIStyle customBoxStyle = new GUIStyle(GUI.skin.box);
            customBoxStyle.padding = new RectOffset(20, 20, 20, 20); // 20 units of padding on all sides

            GUILayout.BeginArea(centeredRect, customBoxStyle); // Begin a new GUI area for the login box using the custom style

            GUILayout.BeginVertical();

            // Title for the Login Box
            GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
            centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Login", centeredLabelStyle);
            GUILayout.Space(10); // Add some space for better visuals

            // Username and Password Fields
            GUILayout.Label("Username");
            username = GUILayout.TextField(username);
            GUILayout.Label("Password");
            password = GUILayout.PasswordField(password, '*');

            // Login Button
            if (GUILayout.Button("Login"))
            {
                await APIRequest.Login(username, password);
                currentUser = await APIRequest.GetCurrentUser();

                if (currentUser != null)
                {
                    username = string.Empty;
                    password = string.Empty;

                    userSatus = UserStatus.LoggedIn;
                }
            }

            // Register Button
            if (GUILayout.Button("Register"))
            {
                signOption = SignOptions.Register;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea(); // End the GUI area
        }


        private async void DrawRegistrationForm()
        {
            // Define custom box style with padding
            GUIStyle customBoxStyle = new GUIStyle(GUI.skin.box);
            customBoxStyle.padding = new RectOffset(20, 20, 20, 20);

            // Center the box on the screen
            float boxWidth = 300;
            float boxHeight = 400;
            float x = (Screen.width - boxWidth) / 2;
            float y = (Screen.height - boxHeight) / 2;

            GUILayout.BeginArea(new Rect(x, y, boxWidth, boxHeight), customBoxStyle);

            GUILayout.BeginVertical();

            // Title for the Registration Box
            GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
            centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Registration", centeredLabelStyle);

            // Input fields
            GUILayout.Label("Username");
            registerUsername = GUILayout.TextField(registerUsername);

            GUILayout.Label("Email");
            registerEmail = GUILayout.TextField(registerEmail);

            GUILayout.Label("Password");
            registerPassword = GUILayout.PasswordField(registerPassword, '*');

            GUILayout.Label("Full Name (Optional)");
            registerFullName = GUILayout.TextField(registerFullName);

            if (GUILayout.Button("Register"))
            {
                var user = await APIRequest.RegisterUser(registerUsername, registerEmail, registerPassword, registerFullName);
                if(debug) Debug.Log(user);
                if(debug) Debug.Log(user.username);
                if(debug) Debug.Log(user.access_token);

                currentUser = await APIRequest.GetCurrentUser();

                if (currentUser != null)
                {
                    registerUsername = string.Empty;
                    registerEmail = string.Empty;
                    registerPassword = string.Empty;
                    registerFullName = string.Empty;

                    userSatus = UserStatus.LoggedIn;
                }
            }
            if (GUILayout.Button("Login"))
            {
                signOption = SignOptions.Login;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private async Task DrawUserInfo()
        {
            if (currentUser != null)
            {

                GUILayout.BeginHorizontal();

                // Aligns the username to the left
                GUILayout.Label(currentUser.username);

                // Add a buy button next to the credits
                if (GUILayout.Button("Logout"))
                {
                    APIRequest.Logout();
                    currentUser = null;
                    username = string.Empty;
                    password = string.Empty;

                    userSatus = UserStatus.LoggedOut;
                }

                // Adds a flexible space which pushes everything after it to the right
                GUILayout.FlexibleSpace();

                // Display the credits
                GUILayout.Label($"Credits: {currentUser.credits}");

                // Add a buy button next to the credits
                if (GUILayout.Button("Buy"))
                {
                    // Handle the button click logic here
                    currentUser = await APIRequest.AddCredits(5);
                    if (debug) Debug.Log(currentUser.credits);
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("User needs to log in.");
                GUILayout.EndHorizontal();
            }

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


        // Log Tabs
        private void DrawErrorLogs()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

                GUILayout.BeginVertical();

                    foreach (string log in log.GetErrorLog().GetQueue())
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

            foreach (string log in log.GetWarningLog().GetQueue())
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

            foreach (string log in log.GetExceptionLog().GetQueue())
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

            foreach (string log in log.GetMessageLog().GetQueue())
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
                    url = APIEndpoints.ProgrammerUrl;
                    break;
                case Assistant.PlotCreator:
                    url = APIEndpoints.PlotUrl;
                    break;
                case Assistant.CharacterCreator:
                    url = APIEndpoints.CharacterUrl;
                    break;
                case Assistant.EnvironmentCreator:
                    url = APIEndpoints.EnvironmentUrl;
                    break;
                case Assistant.StoryCreator:
                    url = APIEndpoints.StoryUrl;
                    break;
                case Assistant.StyleCreator:
                    url = APIEndpoints.StyleUrl;
                    break;
                case Assistant.KeywordExtraction:
                    url = APIEndpoints.KeywordExtractionUrl;
                    break;
                case Assistant.GrammarCorrection:
                    url = APIEndpoints.GrammarCorrectionUrl;
                    break;
                case Assistant.Summarizer:
                    url = APIEndpoints.SummarizationUrl;
                    break;
                case Assistant.Critic:
                    url = APIEndpoints.CriticUrl;
                    break;
                case Assistant.Auto:
                    url = APIEndpoints.AutoUrl;
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
                name = currentUser.username
            };
            AddMessage(message);


            // Create a ChatInputModel
            ChatHistory chat = new ChatHistory
            {
                history = new List<ChatMessage>(chatLog)
            };

            // If debug mode is enabled and there are error messages, add the latest error message to the chat history
            if (applyLogging && log.GetErrorLog().GetQueue().Count > 0)
            {
                var latestError = log.GetErrorLog().GetLatest();

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


            ChatMessage response = await APIRequest.CallPromptedModel(url, chat);

            if (response != null)
                AddMessage(response);
        }
    }
}
