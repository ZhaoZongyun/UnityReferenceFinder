using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ReferencesFindWindow : EditorWindow
{
    private List<string> selectedGuidList = new List<string>();

    //工具栏按钮样式
    private GUIStyle topbarStyle;
    //工具栏样式
    private GUIStyle buttonStyle;
    private AssetTreeView assetTreeView;
    private static ReferenceData reference = new ReferenceData();

    private bool isDepend = false;
    private bool needUpdate = false;

    [MenuItem("Assets/Find References %q", false)]
    static void FindReferences()
    {
        reference.CollectDependenciesInfo();

        ReferencesFindWindow window = GetWindow<ReferencesFindWindow>("Find References");
        window.wantsMouseMove = false;
        window.RefreshWindow();
    }

    private void RefreshWindow()
    {
        selectedGuidList.Clear();
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
            {
                //如果是文件夹，则选择文件夹下所有文件
                string[] guids = AssetDatabase.FindAssets(path);
                foreach (var guid in guids)
                {
                    if (!Directory.Exists(AssetDatabase.GUIDToAssetPath(guid)) && !selectedGuidList.Contains(guid))
                    {
                        selectedGuidList.Add(guid);
                    }
                }
            }
            else
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                selectedGuidList.Add(guid);
            }
        }
        needUpdate = true;
    }

    private void OnEnable()
    {
        topbarStyle = new GUIStyle("Toolbar");
        buttonStyle = new GUIStyle("ToolbarButton");
    }

    private void OnGUI()
    {
        DrawTopBar();
        UpdateAssetTree();
        if (assetTreeView != null)
        {
            assetTreeView.OnGUI(new Rect(0, topbarStyle.fixedHeight, position.width, position.height - topbarStyle.fixedHeight));
        }
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(topbarStyle);
        // 刷新引用信息
        if (GUILayout.Button("Refresh Data", buttonStyle))
        {
            reference.CollectDependenciesInfo(true);
            needUpdate = true;
        }

        bool temp = isDepend;
        isDepend = GUILayout.Toggle(isDepend, isDepend ? "依赖" : "被依赖", buttonStyle, GUILayout.Width(100));

        if (temp != isDepend)
            needUpdate = true;

        GUILayout.FlexibleSpace();

        //展开
        if (GUILayout.Button("Expand", buttonStyle))
            if (assetTreeView != null) assetTreeView.ExpandAll();
        //折叠
        if (GUILayout.Button("Collapse", buttonStyle))
            if (assetTreeView != null) assetTreeView.CollapseAll();

        EditorGUILayout.EndHorizontal();
    }

    private void UpdateAssetTree()
    {
        if (needUpdate && selectedGuidList.Count > 0)
        {
            if (assetTreeView == null)
                assetTreeView = AssetTreeView.Create();

            assetTreeView.assetRoot = CreateRootItem();
            assetTreeView.CollapseAll();
            assetTreeView.Reload();
            needUpdate = false;
        }
    }

    /// <summary>
    /// 创建树根
    /// </summary>
    /// <returns></returns>
    private AssetViewItem CreateRootItem()
    {
        int count = 0;
        var root = new AssetViewItem()
        {
            id = count,
            depth = -1,
            displayName = "Root",
            data = null,
        };

        int depth = 0;
        foreach (var guid in selectedGuidList)
        {
            var item = CreateItem(guid, ref count, depth);
            root.AddChild(item);
        }

        return root;
    }

    private AssetViewItem CreateItem(string guid, ref int count, int depth)
    {
        var desc = reference.GetAndUpdateDescription(guid);
        if (desc == null)
            return null;

        count++;
        var item = new AssetViewItem
        {
            id = count,
            displayName = desc.name,
            depth = depth,
            data = desc,
        };

        var guids = isDepend ? desc.dependencies : desc.beDependencies;
        foreach (var g in guids)
        {
            var childItem = CreateItem(g, ref count, depth + 1);
            item.AddChild(childItem);
        }

        return item;
    }
}

/// <summary>
/// 文件描述
/// </summary>
public class AssetDescription
{
    public string name;
    public string path;
    /// <summary>
    /// 依赖信息hash
    /// </summary>
    public string dependencyHash;
    /// <summary>
    /// 依赖信息（我引用了哪些资源）
    ///     guid列表
    /// </summary>
    public List<string> dependencies = new List<string>();
    /// <summary>
    /// 被依赖信息（哪些资源引用了我）
    ///     guid列表
    /// </summary>
    public List<string> beDependencies = new List<string>();
    public AssetState state = AssetState.normal;
}

public enum AssetState
{
    normal,
    changed,
    missing,
}