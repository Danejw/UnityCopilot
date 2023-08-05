using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityCopilot
{
    public class DragAndDropBag
    {
        public List<Object> droppedFiles = new List<Object>();

        public void HandleDragAndDropEvents()
        {
            Event e = Event.current;
            Rect dropArea = GUILayoutUtility.GetLastRect();

            if (!dropArea.Contains(e.mousePosition))
                return;

            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    string[] folders = DragAndDrop.paths.Where(path => Directory.Exists(path)).ToArray();
                    if (folders.Length > 0)
                    {
                        ProcessDraggedFolders(folders);
                    }
                    else
                    {
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            var path = AssetDatabase.GetAssetPath(obj);
                            if (path.EndsWith(".cs"))
                                droppedFiles.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
                        }
                    }
                }
                e.Use();
            }
        }

        public void ProcessDraggedFolders(string[] folders)
        {
            foreach (string folder in folders)
            {
                string folderPath = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                FileInfo[] csFiles = directoryInfo.GetFiles("*.cs", SearchOption.AllDirectories);

                foreach (FileInfo file in csFiles)
                {
                    string relativePath = "Assets" + file.FullName.Substring(Application.dataPath.Length);
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(relativePath, typeof(UnityEngine.Object));
                    droppedFiles.Add(obj);
                }
            }
        }
    }
}

