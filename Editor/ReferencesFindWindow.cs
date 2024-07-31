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
    private bool needRefreshTree = false;

    [MenuItem("Assets/Find References %q", false, 25)]
    static void FindReferences()
    {
        reference.CollectDependenciesInfo();

        ReferencesFindWindow window = GetWindow<ReferencesFindWindow>("Find References");
        window.wantsMouseMove = false;
        window.RefreshWindow();
    }

    private void OnEnable()
    {
        topbarStyle = new GUIStyle("Toolbar");
        buttonStyle = new GUIStyle("ToolbarButton");
        isDepend = EditorPrefs.GetBool("IsDepend", true);
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
                string[] guids = AssetDatabase.FindAssets("", new string[] { path });
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
        needRefreshTree = true;
    }

    private void OnGUI()
    {
        DrawTopBar();
        UpdateAssetTree();
        assetTreeView.OnGUI(new Rect(0, topbarStyle.fixedHeight, position.width, position.height - topbarStyle.fixedHeight));
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(topbarStyle);
        // 刷新引用信息
        if (GUILayout.Button("Refresh Data", buttonStyle, GUILayout.Width(120)))
        {
            reference.CollectDependenciesInfo(true);
            needRefreshTree = true;
        }

        bool temp = isDepend;
        isDepend = GUILayout.Toggle(isDepend, isDepend ? "依赖" : "被依赖", buttonStyle, GUILayout.Width(120));

        if (temp != isDepend)
        {
            EditorPrefs.SetBool("IsDepend", isDepend);
            needRefreshTree = true;
        }

        GUILayout.FlexibleSpace();

        //展开第一级
        if (GUILayout.Button("ExpandTop", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null)
            {
                foreach (var item in assetTreeView.GetRows())
                    assetTreeView.SetExpanded(item.id, true);
            }

        //展开
        if (GUILayout.Button("ExpandAll", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null) assetTreeView.ExpandAll();

        //折叠
        if (GUILayout.Button("Collapse", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null) assetTreeView.CollapseAll();

        EditorGUILayout.EndHorizontal();
    }

    private void UpdateAssetTree()
    {
        if (needRefreshTree && selectedGuidList.Count > 0)
        {
            if (assetTreeView == null)
                assetTreeView = AssetTreeView.Create();

            assetTreeView.assetRoot = CreateRootItem();
            assetTreeView.CollapseAll();
            assetTreeView.Reload();
            needRefreshTree = false;
        }
    }

    private List<string> childList;

    /// <summary>
    /// 创建树根
    /// </summary>
    /// <returns></returns>
    private AssetViewItem CreateRootItem()
    {
        if (childList == null)
            childList = new List<string>();
        else
            childList.Clear();

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
            if (item != null)
                root.AddChild(item);
        }

        return root;
    }

    private AssetViewItem CreateItem(string guid, ref int count, int depth)
    {
        var desc = reference.GetDescription(guid);
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

        // 防止出现嵌套（A依赖B，B依赖C，C依赖A 的情况）时，造成死循环，出现嵌套时不再添加子节点
        if (!childList.Contains(guid))
        {
            childList.Add(guid);
            var guids = isDepend ? desc.dependencies : desc.beDependencies;
            foreach (var g in guids)
            {
                var childItem = CreateItem(g, ref count, depth + 1);
                if (childItem != null)
                    item.AddChild(childItem);
            }
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