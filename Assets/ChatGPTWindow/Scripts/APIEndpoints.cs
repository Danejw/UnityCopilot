using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCopilot
{
    public static class APIEndpoints
    {
        public const string ChatUrl = "https://localhost:7208/api/OpenAI/ConversationalChatBot";
        public const string ProgrammerUrl = "https://localhost:7208/api/OpenAI/UnityProgrammer";
        public const string StoryDesignerUrl = "https://localhost:7208/api/OpenAI/StoryDesigner";
        public const string CharacterDesignerUrl = "https://localhost:7208/api/OpenAI/CharacterDesigner";
        public const string EnvironmentDesignerUrl = "https://localhost:7208/api/OpenAI/EnvironmentDesigner";
    }
}
