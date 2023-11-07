using UnityEditor;
using System.Collections.Generic;
using System;

namespace Ogxd.ProjectCurator
{
    /// <summary>
    /// The purpose of this class is to try detecting asset changes to automatically update the ProjectCurator database.
    /// </summary>
    public class AssetProcessor : UnityEditor.AssetModificationProcessor
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// Some callbacks must be delayed on next frame
        /// </summary>
        private static void OnUpdate()
        {
            if (Actions.Count > 0) {
                while (Actions.Count > 0) {
                    Actions.Dequeue()?.Invoke();
                }
                ProjectCurator.SaveDatabase();
            }
        }

        private static Queue<Action> Actions = new Queue<Action>();

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (ProjectCuratorData.IsUpToDate) {
                Actions.Enqueue(() => {
                    foreach (string path in paths) {
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        var removedAsset = ProjectCurator.RemoveAssetFromDatabase(guid);
                        ProjectCurator.AddAssetToDatabase(guid, removedAsset?.referencers);
                    }
                });
            }
            return paths;
        }

        static void OnWillCreateAsset(string assetPath)
        {
            if (ProjectCuratorData.IsUpToDate) {
                Actions.Enqueue(() => {
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (guid != string.Empty) {
                        ProjectCurator.AddAssetToDatabase(guid);
                    }
                });
            }
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions removeAssetOptions)
        {
            if (ProjectCuratorData.IsUpToDate) {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (guid != string.Empty) {
                    ProjectCurator.RemoveAssetFromDatabase(guid);
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            return AssetMoveResult.DidNotMove;
        }
    }
}