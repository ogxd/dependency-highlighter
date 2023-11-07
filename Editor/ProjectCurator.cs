using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace Ogxd.ProjectCurator
{
    public static class ProjectCurator
    {
        [NonSerialized]
        private static Dictionary<string, AssetInfo> guidToAssetInfo;

        static ProjectCurator()
        {
            guidToAssetInfo = new Dictionary<string, AssetInfo>();
            try {
                var assetInfos = ProjectCuratorData.AssetInfos;
                for (int i = 0; i < assetInfos.Length; i++) {
                    guidToAssetInfo.Add(assetInfos[i].guid, assetInfos[i]);
                }
            } catch (Exception e) {
                Debug.LogError($"An error occurred while loading ProjectCurator database: {e}");
            }
        }

        public static AssetInfo GetAsset(string guid)
        {
            guidToAssetInfo.TryGetValue(guid, out AssetInfo assetInfo);
            return assetInfo;
        }

        public static AssetInfo AddAssetToDatabase(string guid, HashSet<string> referencers = null)
        {
            AssertGuidValid(guid);

            AssetInfo assetInfo;
            if (!guidToAssetInfo.TryGetValue(guid, out assetInfo)) {
                guidToAssetInfo.Add(guid, assetInfo = new AssetInfo(guid));
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var dependencyPaths = AssetDatabase.GetDependencies(path, recursive: false);

            foreach (string dependencyPath in dependencyPaths) {
                var dependencyGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
                if (
                    dependencyGuid != assetInfo.guid &&
                    guidToAssetInfo.TryGetValue(dependencyGuid, out AssetInfo depInfo)
                ) {
                    assetInfo.dependencies.Add(dependencyGuid);
                    depInfo.referencers.Add(assetInfo.guid);
                    // Included status may have changed and need to be recomputed
                    depInfo.ClearIncludedStatus();
                }
            }

            if (referencers != null)
                assetInfo.referencers = referencers;

            return assetInfo;
        }

        public static AssetInfo RemoveAssetFromDatabase(string guid)
        {
            AssertGuidValid(guid);

            if (guidToAssetInfo.TryGetValue(guid, out AssetInfo assetInfo)) {
                foreach (string referencer in assetInfo.referencers) {
                    if (guidToAssetInfo.TryGetValue(referencer, out AssetInfo referencerAssetInfo)) {
                        if (referencerAssetInfo.dependencies.Remove(guid)) {
                            referencerAssetInfo.ClearIncludedStatus();
                        } else {
                            // Non-Reciprocity Error
                            Warn($"Asset '{FormatGuid(referencer)}' that depends on '{FormatGuid(guid)}' doesn't have it as a dependency");
                        }
                    } else {
                        Warn($"Asset '{FormatGuid(referencer)}' that depends on '{FormatGuid(guid)}' is not present in the database");
                    }
                }
                foreach (string dependency in assetInfo.dependencies) {
                    if (guidToAssetInfo.TryGetValue(dependency, out AssetInfo dependencyAssetInfo)) {
                        if (dependencyAssetInfo.referencers.Remove(guid)) {
                            dependencyAssetInfo.ClearIncludedStatus();
                        } else {
                            // Non-Reciprocity Error
                            Warn($"Asset '{FormatGuid(dependency)}' that is referenced by '{FormatGuid(guid)}' doesn't have it as a referencer");
                        }
                    } else {
                        Warn($"Asset '{FormatGuid(dependency)}' that is referenced by '{FormatGuid(guid)}' is not present in the database");
                    }
                }
                guidToAssetInfo.Remove(guid);
            } else {
                Warn($"Asset '{FormatGuid(guid)}' is not present in the database");
            }

            return assetInfo;
        }

        public static void ClearDatabase()
        {
            guidToAssetInfo.Clear();
        }

        public static void RebuildDatabase()
        {
            guidToAssetInfo = new Dictionary<string, AssetInfo>();

            var allAssetPaths = AssetDatabase.GetAllAssetPaths();

            // Ignore non-assets (package folder for instance) and directories
            allAssetPaths = allAssetPaths
                .Where(path => path.StartsWith("Assets/") && !Directory.Exists(path))
                .ToArray();

            EditorUtility.DisplayProgressBar("Building Dependency Database", "Gathering All Assets...", 0f);

            // Gather all assets
            for (int p = 0; p < allAssetPaths.Length; p++) {
                string path = allAssetPaths[p];
                string guid = AssetDatabase.AssetPathToGUID(path);
                AssetInfo assetInfo = new AssetInfo(guid);
                guidToAssetInfo.Add(assetInfo.guid, assetInfo);
            }

            // Find links between assets
            for (int p = 0; p < allAssetPaths.Length; p++) {
                var path = allAssetPaths[p];
                if (p % 10 == 0) {
                    var cancel = EditorUtility.DisplayCancelableProgressBar("Building Dependency Database", path, (float)p / allAssetPaths.Length);
                    if (cancel) {
                        guidToAssetInfo = null;
                        break;
                    }
                }
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddAssetToDatabase(guid);
            }

            EditorUtility.ClearProgressBar();

            ProjectCuratorData.IsUpToDate = true;

            SaveDatabase();
        }

        public static void SaveDatabase()
        {
            if (guidToAssetInfo == null)
                return;
            var assetInfos = new AssetInfo[guidToAssetInfo.Count];
            int i = 0;
            foreach (var pair in guidToAssetInfo) {
                assetInfos[i] = pair.Value;
                i++;
            }
            ProjectCuratorData.AssetInfos = assetInfos;
            ProjectCuratorData.Save();
        }

        static void Warn(string message)
        {
            Debug.LogWarning("ProjectCurator: " + message);
        }

        static string FormatGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path)
                ? $"(Missing asset with GUID={guid})"
                : path;
        }

        static void AssertGuidValid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) {
                throw new ArgumentException("GUID required", nameof(guid));
            }
        }
    }
}