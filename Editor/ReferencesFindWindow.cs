using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class ReferencesFindWindow : EditorWindow
{
    public enum SortMode
    {
        path,   // 按资源路径排序
        name,   // 按资源名称排序
        type,   // 按资源类型路径排序
    }

    /// <summary>
    /// 选择的对象
    /// </summary>
    private List<string> selectedGuidList = new List<string>();

    //工具栏按钮样式
    private GUIStyle topbarStyle;
    //工具栏样式
    private GUIStyle buttonStyle;
    private AssetTreeView assetTreeView;
    private static ReferenceData data = new ReferenceData();

    private bool isDepend = false;
    private bool needRefreshTree = false;

    private bool isTile = true;
    private List<string> resultList = new List<string>();
    private List<string> recursiveList = new List<string>();

    private SortMode sortMode = SortMode.path;

    [MenuItem("Assets/Find References &q", false, 25)]
    static void FindReferences()
    {
        data.CollectDependenciesInfo();

        ReferencesFindWindow window = GetWindow<ReferencesFindWindow>("Find References");
        window.wantsMouseMove = false;
        window.RefreshWindow();
    }

    private void OnEnable()
    {
        topbarStyle = new GUIStyle("Toolbar");
        topbarStyle.fixedHeight = 30;
        buttonStyle = new GUIStyle("ToolbarButton");
        buttonStyle.fixedHeight = 30;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
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
        if (assetTreeView != null)
            assetTreeView.OnGUI(new Rect(0, topbarStyle.fixedHeight, position.width, position.height - topbarStyle.fixedHeight));
    }

    private void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(topbarStyle);
        // 刷新引用信息
        if (GUILayout.Button("刷新", buttonStyle, GUILayout.Width(120)))
        {
            data.CollectDependenciesInfo(true);
            needRefreshTree = true;
        }

        bool temp = isDepend;
        isDepend = GUILayout.Toggle(isDepend, isDepend ? "依赖" : "被依赖", buttonStyle, GUILayout.Width(120));
        if (temp != isDepend)
        {
            EditorPrefs.SetBool("IsDepend", isDepend);
            needRefreshTree = true;
        }

        if (GUILayout.Button("路径排序", buttonStyle, GUILayout.Width(120)))
        {
            if (sortMode!= SortMode.path)
            {
                sortMode = SortMode.path;
                needRefreshTree = true;
            }
        }

        if (GUILayout.Button("名称排序", buttonStyle, GUILayout.Width(120)))
        {
            if (sortMode != SortMode.name)
            {
                sortMode = SortMode.name;
                needRefreshTree = true;
            }
        }

        if (GUILayout.Button("类型排序", buttonStyle, GUILayout.Width(120)))
        {
            if (sortMode != SortMode.type)
            {
                sortMode = SortMode.type;
                needRefreshTree = true;
            }
        }

        GUILayout.FlexibleSpace();

        //平铺
        temp = isTile;
        isTile = GUILayout.Toggle(isTile, isTile ? "平铺(递归)" : "树形", buttonStyle, GUILayout.Width(120));
        if (temp != isTile)
        {
            EditorPrefs.SetBool("IsTile", isTile);
            needRefreshTree = true;
        }

        //展开第一级
        if (GUILayout.Button("展开第一级", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null)
            {
                foreach (var item in assetTreeView.GetRows())
                    assetTreeView.SetExpanded(item.id, true);
            }

        //展开
        if (GUILayout.Button("全部展开", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null) assetTreeView.ExpandAll();

        //折叠
        if (GUILayout.Button("全部折叠", buttonStyle, GUILayout.Width(120)))
            if (assetTreeView != null) assetTreeView.CollapseAll();

        EditorGUILayout.EndHorizontal();
    }

    private void UpdateAssetTree()
    {
        if (needRefreshTree && selectedGuidList.Count > 0)
        {
            if (assetTreeView == null)
                assetTreeView = AssetTreeView.Create();

            if (isTile)
                assetTreeView.assetRoot = CreateRootItemTile();
            else
                assetTreeView.assetRoot = CreateRootItemTree();

            assetTreeView.CollapseAll();
            assetTreeView.Reload();
            if (!isTile)
            {  //展开第一级
                foreach (var item in assetTreeView.GetRows())
                    assetTreeView.SetExpanded(item.id, true);
            }

            needRefreshTree = false;
        }
    }

    /// <summary>
    /// 创建树根（平铺）
    /// </summary>
    /// <returns></returns>
    private AssetViewItem CreateRootItemTile()
    {
        int count = -1;
        var root = NewItem(null, ref count, "Root", -1, null);

        NewItem(root, ref count, "===== 选中的资源 ===== " + selectedGuidList.Count, 0, null);

        resultList.Clear();
        foreach (var guid in selectedGuidList)
        {
            var desc = data.GetDescription(guid);
            if (desc != null)
            {
                NewItem(root, ref count, desc.name, 0, desc);

                // 平铺时，展示的递归的结果（依赖的依赖，被依赖的被依赖，全部查找出来）
                recursiveList.Clear();
                if (isDepend)
                    Recursive(desc.dependencies, recursiveList);
                else
                    Recursive(desc.beDependencies, recursiveList);

                // 去重
                foreach (var g in recursiveList)
                {
                    if (!resultList.Contains(g))
                        resultList.Add(g);
                }
            }
        }

        // 排序后展示列表
        resultList.Sort((string guidA, string guidB) =>
        {
            var desc1 = data.GetDescription(guidA);
            var desc2 = data.GetDescription(guidB);

            if (desc1 != null && desc2 != null) {

                switch (sortMode)
                {
                    case SortMode.path:
                        return desc1.path.CompareTo(desc2.path);
                    case SortMode.name:
                        return desc1.name.CompareTo(desc2.name);
                    case SortMode.type:
                        return desc1.ext.CompareTo(desc2.ext);
                }
            }
            return 0;
        });

        if (isDepend)
            NewItem(root, ref count, "===== 依赖 ===== " + resultList.Count, 0, null);
        else
            NewItem(root, ref count, "===== 被依赖 ===== " + resultList.Count, 0, null);

        foreach (var result in resultList)
        {
            var desc = data.GetDescription(result);
            if (desc != null)
                NewItem(root, ref count, desc.name, 0, desc);
            else
            {
                Debug.Log("找不到 Guid " + desc.path);
            }
        }

        return root;
    }

    // 递归获取所有引用
    private void Recursive(List<string> guidList, List<string> tempList)
    {
        for (int i = 0; i < guidList.Count; i++)
        {
            var desc = data.GetDescription(guidList[i]);
            if (desc != null)
            {
                tempList.Add(guidList[i]);
                if (isDepend)
                {
                    if (desc.dependencies.Count > 0)
                    {
                        Recursive(desc.dependencies, tempList);
                    }
                }
                else
                {
                    if (desc.beDependencies.Count > 0)
                    {
                        Recursive(desc.beDependencies, tempList);
                    }
                }
            }
        }
    }

    private AssetViewItem NewItem(AssetViewItem parent, ref int count, string name, int depth, AssetDescription desc)
    {
        count++;
        var item = new AssetViewItem
        {
            id = count,
            displayName = name,
            depth = depth,
            data = desc,
        };

        if (parent != null)
            parent.AddChild(item);
        return item;
    }

    private List<string> childList;

    /// <summary>
    /// 创建树根（树形）
    /// </summary>
    /// <returns></returns>
    private AssetViewItem CreateRootItemTree()
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
        var desc = data.GetDescription(guid);
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

        // 防止出现嵌套（A依赖B，B依赖C，C依赖A 的情况）时，造成死循环，出现嵌套时不再向下添加子节点
        if (!childList.Contains(guid))
        {
            childList.Add(guid);
            desc.dependencies.Sort();

            // 排序
            if (isDepend)
                desc.dependencies.Sort((string a, string b) =>
                {
                    var desc1 = data.GetDescription(a);
                    var desc2 = data.GetDescription(b);
                    if (desc1 != null && desc2 != null)
                    {
                        switch (sortMode)
                        {
                            case SortMode.path:
                                return desc1.path.CompareTo(desc2.path);
                            case SortMode.name:
                                return desc1.name.CompareTo(desc2.name);
                            case SortMode.type:
                                return desc1.ext.CompareTo(desc2.ext);
                        }
                    }
                    return 0;
                });
            else
                desc.beDependencies.Sort((string a, string b) =>
                {
                    var desc1 = data.GetDescription(a);
                    var desc2 = data.GetDescription(b);
                    if (desc1 != null && desc2 != null)
                    {
                        switch (sortMode)
                        {
                            case SortMode.path:
                                return desc1.path.CompareTo(desc2.path);
                            case SortMode.name:
                                return desc1.name.CompareTo(desc2.name);
                            case SortMode.type:
                                return desc1.ext.CompareTo(desc2.ext);
                        }
                    }
                    return 0;
                });

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
    public string ext;
    /// <summary>
    /// 依赖信息hash
    /// </summary>
    public string dependencyHash;
    /// <summary>
    /// 依赖信息（我引用了哪些资源）,包括依赖的依赖
    ///     guid列表
    /// </summary>
    public List<string> dependencies = new List<string>();
    /// <summary>
    /// 被依赖信息（哪些资源引用了我）
    ///     guid列表
    /// </summary>
    public List<string> beDependencies = new List<string>();
    public AssetState state = AssetState.normal;
    public string note;

    public override string ToString()
    {
        return path;
    }
}

public enum AssetState
{
    normal,
    /// <summary>
    /// 依赖信息改变
    /// </summary>
    changed,
    /// <summary>
    /// 文件不存在
    /// </summary>
    missing,
    /// <summary>
    /// AssetDatabase 加载不到
    /// </summary>
    invalid,
}