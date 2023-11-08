using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Ogxd.ProjectCurator
{
    public enum IncludedInBuild
    {
        Unknown = 0,
        // Not included in build
        NotIncludable = 1,
        NotIncluded = 2,
        // Included in build
        SceneInBuild = 10,
        RuntimeScript = 11,
        ResourceAsset = 12,
        Referenced = 13,
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AssetInfo
    {
        [JsonProperty]
        public GUID guid;

        [JsonProperty]
        public HashSet<GUID> referencers = new();

        [JsonProperty]
        public HashSet<GUID> dependencies = new();

        IncludedInBuild _includedStatus;

        public AssetInfo(GUID guid)
        {
            this.guid = guid;
        }

        public void ClearIncludedStatus()
        {
            _includedStatus = IncludedInBuild.Unknown;
        }

        public IncludedInBuild IncludedStatus {
            get {
                if (_includedStatus != IncludedInBuild.Unknown)
                    return _includedStatus;
                // Avoid circular loops
                _includedStatus = IncludedInBuild.NotIncluded;
                return _includedStatus = CheckIncludedStatus();
            }
        }

        public bool IsIncludedInBuild => (int)IncludedStatus >= 10;

        private IncludedInBuild CheckIncludedStatus()
        {

            foreach (var referencer in referencers) {
                AssetInfo refInfo = ProjectCurator.GetAsset(referencer);
                if (refInfo.IsIncludedInBuild) {
                    return IncludedInBuild.Referenced;
                }
            }

            bool isInEditor = false;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            string[] directories = path.ToLower().Split('/');
            for (int i = 0; i < directories.Length - 1; i++) {
                switch (directories[i]) {
                    case "editor":
                        isInEditor = true;
                        break;
                    case "resources":
                        return IncludedInBuild.ResourceAsset;
                    case "plugins":
                        break;
                    default:
                        break;
                }
            }

            string extension = System.IO.Path.GetExtension(path);
            switch (extension) {
                case ".cs":
                    if (isInEditor) {
                        return IncludedInBuild.NotIncludable;
                    } else {
                        return IncludedInBuild.RuntimeScript;
                    }
                case ".unity":
                    if (EditorBuildSettings.scenes.Select(x => x.path).Contains(path))
                        return IncludedInBuild.SceneInBuild;
                    break;
                // Todo : Handle DLL
                // https://docs.unity3d.com/ScriptReference/Compilation.Assembly-compiledAssemblyReferences.html
                // CompilationPipeline
                // Assembly Definition
                default:
                    break;
            }

            return IncludedInBuild.NotIncluded;
        }
    }
}