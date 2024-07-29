using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorGlobal : MonoBehaviour
{
    static EditorGlobal()
    {
        OnInitProject();
    }

    private static void OnInitProject()
    {
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
    }

    /// <summary>
    /// 头部GUI
    /// </summary>
    /// <param name="editor"></param>
    private static void OnFinishedDefaultHeaderGUI(Editor editor)
    {
        if (editor.targets != null && editor.targets.Length > 1)
            return;

        string path = AssetDatabase.GetAssetPath(editor.target);
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
        {
            UnityEngine.Debug.LogError($"路径错误 {path}");
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open with VSCode", EditorStyles.miniButton))
        {
            EditWithVSCode(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/") + 1) + AssetDatabase.GetAssetPath(editor.target));
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 打开 VSCode 编辑
    /// </summary>
    private static void EditWithVSCode(string filePath)
    {
        string vscodePath = "D:/Program Files/Microsoft VS Code/Code.exe";

        if (!File.Exists(vscodePath))
        {
            UnityEngine.Debug.LogError($"没有在 {vscodePath} 找到 vscode");
            return;
        }

        Process process = new Process();
        process.StartInfo = new ProcessStartInfo(vscodePath, filePath);
        process.Start();
    }
}
