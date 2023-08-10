using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCopilot
{
    public static class APIEndpoints
    {
        // ASP.Net Server
        public const string ChatUrl = "https://localhost:7208/api/OpenAI/ConversationalChatBot";
        public const string ProgrammerUrl = "https://localhost:7208/api/OpenAI/UnityProgrammer";
        public const string StoryDesignerUrl = "https://localhost:7208/api/OpenAI/StoryDesigner";
        public const string CharacterDesignerUrl = "https://localhost:7208/api/OpenAI/CharacterDesigner";
        public const string EnvironmentDesignerUrl = "https://localhost:7208/api/OpenAI/EnvironmentDesigner";

        // Python Server
        public const string ProgrammerPythonUrl = "http://127.0.0.1:8000/programmer/";
        public const string PlotPythonUrl = "http://127.0.0.1:8000/plot/";
        public const string CharacterPythonUrl = "http://127.0.0.1:8000/character/";
        public const string EnvironmentPythonUrl = "http://127.0.0.1:8000/environment/";
        public const string StoryPythonUrl = "http://127.0.0.1:8000/story/";
    }
}
