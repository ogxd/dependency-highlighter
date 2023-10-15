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
        private static readonly Queue<Action> _actions = new Queue<Action>();

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
            if (_actions.Count > 0) {
                while (_actions.Count > 0) {
                    _actions.Dequeue()?.Invoke();
                }
                ProjectCurator.SaveDatabase();
            }
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (ProjectCuratorData.IsUpToDate) {
                _actions.Enqueue(() => {
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
                _actions.Enqueue(() => {
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