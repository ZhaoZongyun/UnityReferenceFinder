using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 依赖、引用数据
/// </summary>
public class ReferenceData
{
    private const string CACHE_PATH = "Library/ReferenceFinderCache";
    public Dictionary<string, AssetDescription> assetDict = new Dictionary<string, AssetDescription>();

    /// <summary>
    /// 收集所有文件的依赖信息（Assets目录下的所有文件）
    /// </summary>
    public void CollectDependenciesInfo(bool forceUpdate = false)
    {
        if (!forceUpdate && ReadFromCache())
            //非强制更新且从缓存中读取到，则使用缓存中的信息
            return;

        //否则重新收集信息
        assetDict.Clear();
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int length = allAssetPaths.Length;
        int count = 0;

        try
        {
            for (int i = 0; i < length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Collecting Dependencies", $"Collecting {i + 1} asset", (float)(i + 1) / length))
                    break;

                var path = allAssetPaths[i];
                if (path.StartsWith("Assets/") && File.Exists(path))
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    Hash128 hash = AssetDatabase.GetAssetDependencyHash(path);

                    if (!assetDict.ContainsKey(guid) || assetDict[guid].dependencyHash != hash.ToString())
                    {
                        var guids = AssetDatabase.GetDependencies(path, false).Select(p => AssetDatabase.AssetPathToGUID(p)).ToList();

                        AssetDescription desc = new AssetDescription();
                        desc.name = Path.GetFileName(path);
                        desc.path = path;
                        desc.dependencyHash = hash.ToString();
                        desc.dependencies = guids;

                        if (assetDict.ContainsKey(guid))
                            assetDict[guid] = desc;
                        else
                        {
                            count++;
                            assetDict.Add(guid, desc);
                        }

                    }
                }
            }

            Debug.Log($"收集了{count}个资源的依赖信息");
            EditorUtility.ClearProgressBar();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            throw;
        }

        WriteToCache();

        CollectReferencesInfo();
    }

    /// <summary>
    /// 收集被依赖信息
    /// </summary>
    private void CollectReferencesInfo()
    {
        foreach (var asset in assetDict)
        {
            foreach (var guid in asset.Value.dependencies)
            {
                if (assetDict.ContainsKey(guid) && !assetDict[guid].beDependencies.Contains(asset.Key))
                {
                    assetDict[guid].beDependencies.Add(asset.Key);
                }
            }
        }
    }

    public AssetDescription GetDescription(string guid)
    {
        if (assetDict.ContainsKey(guid))
        {
            var desc = assetDict[guid];
            if (File.Exists(desc.path))
            {
                if (AssetDatabase.LoadAssetAtPath(desc.path, typeof(Object)) == null)
                    desc.state = AssetState.invalid;
                else
                {
                    if (desc.dependencyHash != AssetDatabase.GetAssetDependencyHash(desc.path).ToString())
                        desc.state = AssetState.changed;
                    else
                        desc.state = AssetState.normal;
                }
            }
            else
                desc.state = AssetState.missing;

            return desc;
        }
        return null;
    }

    /// <summary>
    /// 从缓存读取
    /// </summary>
    /// <returns></returns>
    private bool ReadFromCache()
    {
        if (!File.Exists(CACHE_PATH))
            return false;

        // 文件列表
        List<string> guidList = new List<string>();
        // 依赖hash列表
        List<string> dependencyHashList = new List<string>();
        // 被依赖索引数组列表
        List<int[]> dependencyList = new List<int[]>();

        using (FileStream fs = File.OpenRead(CACHE_PATH))
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                EditorUtility.DisplayCancelableProgressBar("Reading Cache", "Reading Cache", 0);
                guidList = (List<string>)bf.Deserialize(fs);
                dependencyHashList = (List<string>)bf.Deserialize(fs);
                dependencyList = (List<int[]>)bf.Deserialize(fs);
            }
            catch (System.Exception)
            {
                EditorUtility.ClearProgressBar();
                throw;
            }

            EditorUtility.ClearProgressBar();
        }

        assetDict.Clear();
        for (int i = 0; i < guidList.Count; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guidList[i]);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDescription desc = new AssetDescription();
                desc.name = Path.GetFileName(path);
                desc.path = path;
                desc.dependencyHash = dependencyHashList[i];
                assetDict.Add(guidList[i], desc);
            }
        }

        for (int i = 0; i < guidList.Count; i++)
        {
            string guid = guidList[i];
            if (assetDict.ContainsKey(guid))
            {
                var list = dependencyList[i].Select(index => guidList[index]).Where(g => assetDict.ContainsKey(guid)).ToList();
                assetDict[guid].dependencies = list;
            }
        }

        CollectReferencesInfo();

        return true;
    }

    /// <summary>
    /// 写入缓存
    /// </summary>
    private void WriteToCache()
    {
        if (File.Exists(CACHE_PATH))
            File.Delete(CACHE_PATH);

        // 文件列表
        List<string> guidList = new List<string>();
        // 依赖hash列表
        List<string> dependencyHashList = new List<string>();
        // 被依赖索引数组列表
        List<int[]> dependencyList = new List<int[]>();

        //guid - index 映射
        Dictionary<string, int> guidIndexDict = new Dictionary<string, int>();
        using (FileStream fs = File.OpenWrite(CACHE_PATH))
        {
            foreach (var pair in assetDict)
            {
                var guid = pair.Key;
                guidIndexDict.Add(guid, guidIndexDict.Count);
                guidList.Add(guid);
                dependencyHashList.Add(pair.Value.dependencyHash);
            }

            foreach (var guid in guidList)
            {
                int[] indexes = assetDict[guid].dependencies.Where(g => guidIndexDict.ContainsKey(g)).Select(g => guidIndexDict[g]).ToArray();
                dependencyList.Add(indexes);
            }

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, guidList);
            bf.Serialize(fs, dependencyHashList);
            bf.Serialize(fs, dependencyList);
        }
    }
}
