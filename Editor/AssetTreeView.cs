using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class AssetViewItem : TreeViewItem
{
    public AssetDescription data;
}

public class AssetTreeView : TreeView
{
    private const float ICON_WIDTH = 18f;
    private const float ROW_HEIGHT = 20f;
    public AssetViewItem assetRoot;

    protected override TreeViewItem BuildRoot()
    {
        return assetRoot;
    }

    /// <summary>
    /// 绘制树
    /// </summary>
    /// <param name="args"></param>
    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (AssetViewItem)args.item;
        for (int i = 0; i < args.GetNumVisibleColumns(); i++)
        {
            DrawItem(args.GetCellRect(i), item, (Colum)args.GetColumn(i), ref args);
        }
    }

    enum Colum
    {
        Name,
        Path,
        State,
    }

    private GUIStyle stateStyle = new GUIStyle { richText = true, alignment = TextAnchor.MiddleCenter };

    private void DrawItem(Rect cellRect, AssetViewItem item, Colum colum, ref RowGUIArgs args)
    {
        switch (colum)
        {
            case Colum.Name:
                var iconRect = cellRect;
                iconRect.x += GetContentIndent(item);
                iconRect.width = ICON_WIDTH;
                if (iconRect.x < cellRect.xMax)
                {
                    var icon = GetIcon(item.data.path);
                    if (icon != null)
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                }
                break;
            case Colum.Path:
                GUI.Label(cellRect, item.data.path);
                break;
            case Colum.State:
                GUI.Label(cellRect, GetStateString(item.data.state), stateStyle);
                break;
        }
    }

    private string GetStateString(AssetState assetState)
    {
        switch (assetState)
        {
            case AssetState.changed:
                return "<color=#F0672AFF>Changed</color>";
            case AssetState.missing:
                return "<color=#FF0000FF>Missing</color>";
        }

        return "Normal";
    }

    private Texture2D GetIcon(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
        if (asset != null)
        {
            Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
            if (icon == null)
                icon = AssetPreview.GetMiniTypeThumbnail(asset.GetType());
            return icon;
        }

        return null;
    }

    /// <summary>
    /// 创建树
    /// </summary>
    /// <returns></returns>
    public static AssetTreeView Create()
    {
        TreeViewState state = new TreeViewState();
        var headerState = CreateColumHeaderState();
        var multiColumHeader = new MultiColumnHeader(headerState);

        return new AssetTreeView(state, multiColumHeader);
    }

    private AssetTreeView(TreeViewState state, MultiColumnHeader headerState) : base(state, headerState)
    {
        rowHeight = ROW_HEIGHT;
        columnIndexForTreeFoldouts = 0;
        showAlternatingRowBackgrounds = true;
        showBorder = true;
        customFoldoutYOffset = (ROW_HEIGHT - EditorGUIUtility.singleLineHeight) * 0.5f;
        extraSpaceBeforeIconAndLabel = ROW_HEIGHT;
    }

    /// <summary>
    /// 创建列头部
    /// </summary>
    /// <returns></returns>
    private static MultiColumnHeaderState CreateColumHeaderState()
    {
        var colums = new[] {
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Name"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 200,
                minWidth = 60,
                autoResize = false,
                allowToggleVisibility = false,
                canSort = false,
            },
            new MultiColumnHeaderState.Column{
                headerContent = new GUIContent("Path"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 360,
                minWidth = 60,
                autoResize = false,
                allowToggleVisibility = true,
                canSort = false,
            },
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("State"),
                headerTextAlignment = TextAlignment.Center,
                sortedAscending = false,
                width = 60,
                minWidth = 60,
                autoResize = false,
                allowToggleVisibility = true,
                canSort = false,
            }
        };

        var state = new MultiColumnHeaderState(colums);
        return state;
    }

    /// <summary>
    /// 双击事件
    /// </summary>
    /// <param name="id"></param>
    protected override void DoubleClickedItem(int id)
    {
        var item = (AssetViewItem)FindItem(id, rootItem);
        if (item != null)
        {
            var asset = AssetDatabase.LoadAssetAtPath(item.data.path, typeof(UnityEngine.Object));
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
