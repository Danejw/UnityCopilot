using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


namespace UnityCopilot.Utils
{
    public static class FileUtils
    {
        public static string ExtractScriptPathFromError(string errorMessage)
        {
            Match match = Regex.Match(errorMessage, @"(Assets\\[^(:]*)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        public static string ReadScriptContentFromPath(string scriptPath)
        {
            // Validate that the script path is within the Assets directory
            if (!scriptPath.StartsWith("Assets\\"))
            {
                Debug.LogError("Unauthorized access to sensitive files: " + scriptPath);
                return null;
            }

            // Validate that the scriptPath points to a .cs file
            if (!scriptPath.EndsWith(".cs"))
            {
                Debug.LogError("File is not a C# script: " + scriptPath);
                return null;
            }

            // Read the script file content
            try
            {
                // Convert the relative script path to an absolute path
                string absoluteScriptPath = Path.Combine(Application.dataPath, scriptPath.Substring(7));
                return System.IO.File.ReadAllText(absoluteScriptPath);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to read script content: " + e.Message);
                return null;
            }
        }

        public static string ReadCSharpFile(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("The object is not a valid asset.");
                return null;
            }

            if (!path.EndsWith(".cs"))
            {
                Debug.LogError("The object is not a C# script.");
                return null;
            }

            try
            {
                string content = File.ReadAllText(path);

                // Remove all spaces and new lines
                //content = content.Replace(" ", "").Replace("\n", "").Replace("\r", "");

                Debug.Log(content);
                return content;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read script content: " + e.Message);
                return null;
            }
        }

        // This regular expression tries to match methods in a C# file.
        // It's simplistic and won't handle edge cases.
        private static readonly string methodPattern = @"(public|private|protected|static|void|\w+ \w+)(\s+\w+\s*)\(([^)]*)\)\s*{";

        public static List<string> CSharpCodeSpliter(UnityEngine.Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("The object is not a valid asset.");
                return null;
            }

            if (!path.EndsWith(".cs"))
            {
                Debug.LogError("The object is not a C# script.");
                return null;
            }

            try
            {
                string content = File.ReadAllText(path);

                var matches = Regex.Matches(content, methodPattern);
                var methods = new List<string>();

                for (int i = 0; i < matches.Count; i++)
                {
                    int startIndex = matches[i].Index;
                    int endIndex = (i < matches.Count - 1) ? matches[i + 1].Index : content.Length;

                    methods.Add(content.Substring(startIndex, endIndex - startIndex));
                }

                foreach(var method in methods)
                {
                    Debug.Log(method);
                }

                return methods;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read script content: " + e.Message);
                return null;
            }
        }

    }
}
