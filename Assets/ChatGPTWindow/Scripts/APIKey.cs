using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityCopilot
{
    public static class APIKey
    {
        private static string userAPIKey { get; set; }

        public static string GetAPIKey() { return userAPIKey; }
        public static void SetAPIKey(string apiKey) {  userAPIKey = apiKey; }
    }

}
