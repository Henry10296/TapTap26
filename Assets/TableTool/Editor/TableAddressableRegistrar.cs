using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace TableTool.Editor
{
    /// <summary>导出的 JSON 自动注册进 Addressables：Configs group，地址 = "Configs/<表名>"。</summary>
    public static class TableAddressableRegistrar
    {
        const string GroupName = "Configs";
        const string AddressPrefix = "Configs/";

        /// <summary>注册一张已导出的 JSON 表；返回 null = 成功，否则为错误信息（不抛异常）。</summary>
        public static string Register(string assetPath, string tableName)
        {
            try
            {
                // 首次使用时初始化默认 Settings（Assets/AddressableAssetsData/AddressableAssetSettings.asset）
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    settings = AddressableAssetSettings.Create(
                        AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                        AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
                    Debug.Log("[导表] 已创建默认 Addressables Settings");
                }

                var group = settings.FindGroup(GroupName);
                if (group == null)
                {
                    group = settings.CreateGroup(GroupName, false, false, false, null,
                        typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                    Debug.Log("[导表] 已创建 Addressables group：Configs");
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                    return $"无法获取资源 GUID：{assetPath}（文件尚未被 Unity 导入）";

                var entry = settings.CreateOrMoveEntry(guid, group);
                if (entry == null)
                    return $"创建 Addressable 条目失败：{assetPath}";

                entry.address = AddressPrefix + tableName;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
                return null;
            }
            catch (Exception e)
            {
                return $"Addressables 注册失败：{e.Message}";
            }
        }
    }
}
