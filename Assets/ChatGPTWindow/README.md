
# Unity Copilot Plugin

This plugin assists developers by integrating a chat interface in the Unity editor. It communicates with external services to provide real-time feedback and suggestions based on the user's input and the context of their project.

## Files Overview

### 1. ChatWindow.cs
- **Purpose**: Manages the main chat window interface in Unity's editor.
- **Key Classes**:
    - `ChatWindow`
- **Key Methods**:
    - `ShowWindow`: Opens the chat window.
    - `OnGUI`: Renders the GUI.
    - `DrawMainToolbar`: Draws the main toolbar.
    - `DrawChatTab`: Manages the chat tab interface.
    - `AddMessage`: Adds a message to the chat.
    - `RemoveMessage`: Removes a message.
    - `HandleLog`: Logs Unity messages.
    - `SetUpMessage`: Prepares a message for the API.
    - `SendAPIRequest`: Sends an API request.

### 2. APIEndpoints.cs
- **Purpose**: Provides endpoints for various API services.
- **Key Classes**:
    - `APIEndpoints`

### 3. APIRequest.cs
- **Purpose**: Manages API requests.
- **Key Classes**:
    - `APIRequest`
- **Key Methods**:
    - `SendAPIRequest`: Sends a request to an API.

### 4. ChatMessage.cs
- **Purpose**: Defines structures for chat interactions.
- **Key Classes**:
    - `ChatMessage`
    - `ChatInputModel`

### 5. CustomLogger.cs
- **Purpose**: Handles and categorizes Unity log messages.
- **Key Classes**:
    - `CustomLogger`
- **Key Methods**:
    - `LogFormat`: Categorizes log messages.
    - `GetLatestError/Warning/Message`: Retrieves the latest log messages.

### 6. DragAndDropBag.cs
- **Purpose**: Manages drag and drop functionality in the Unity editor.
- **Key Classes**:
    - `DragAndDropBag`
- **Key Methods**:
    - `HandleDragAndDropEvents`: Manages drag and drop events.
    - `ProcessDraggedFolders`: Processes dragged files.

### 7. FileUtils.cs
- **Purpose**: Provides utilities for working with files.
- **Key Classes**:
    - `FileUtils`
- **Key Methods**:
    - `ExtractScriptPathFromError`: Extracts a script's path from an error.
    - `ReadScriptContentFromPath`: Reads a script's content.
    - `ReadCSharpFile`: Reads a C# file's content.

## Getting Started

1. Install the unity package by importing it into your Unity project.
2. Navigate to `Tools > UnityCopilot` to open the chat window.
3. Start chatting with the assistant to get feedback and suggestions.

## Contact

For issues, suggestions, or contributions, please open an issue on the GitHub repository.

