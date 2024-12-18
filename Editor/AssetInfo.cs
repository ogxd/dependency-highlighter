using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
#if USING_ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

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

#if USING_ADDRESSABLES
        private static string s_addressablesSettingsPath;
        public static string AddressablesSettingsPath
        {
	        get
	        {
		        if (s_addressablesSettingsPath != null)
			        return s_addressablesSettingsPath;
		        if (AddressableAssetSettingsDefaultObject.SettingsExists)
		        {
			        s_addressablesSettingsPath = AssetDatabase.GetAssetPath(AddressableAssetSettingsDefaultObject.Settings);
		        }
		        return s_addressablesSettingsPath;
	        }
        }
#endif

        private IncludedInBuild CheckIncludedStatus()
        {
			// Check the calculated references, later we check the remaining references.
			// This is done in this order to prevent loops from asserting this asset isn't referenced if it's the root.
            foreach (var referencer in referencers) {
                AssetInfo refInfo = ProjectCurator.GetAsset(referencer);
                if (refInfo._includedStatus != IncludedInBuild.Unknown && refInfo.IsIncludedInBuild) {
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
            
#if USING_ADDRESSABLES
	        if (path == AddressablesSettingsPath)
		        return IncludedInBuild.ResourceAsset;
#endif
	        
	        // Check remaining referencers that weren't calculated.
	        foreach (var referencer in referencers) {
		        AssetInfo refInfo = ProjectCurator.GetAsset(referencer);
		        if (refInfo._includedStatus == IncludedInBuild.Unknown && refInfo.IsIncludedInBuild) {
			        return IncludedInBuild.Referenced;
		        }
	        }

            return IncludedInBuild.NotIncluded;
        }
    }
}