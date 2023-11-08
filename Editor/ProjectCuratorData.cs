using Newtonsoft.Json;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ogxd.ProjectCurator
{
    class GuidConverter : JsonConverter<GUID>
    {
        public override GUID ReadJson(JsonReader reader, Type objectType, GUID existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            GUID.TryParse(reader.Value as string, out var result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, GUID value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ProjectCuratorData
    {
        private const string JSON_PATH = "UserSettings/ProjectCuratorData.json";

        [JsonProperty]
        bool isUpToDate = false;

        public static bool IsUpToDate {
            get => Instance.isUpToDate;
            set => Instance.isUpToDate = value;
        }

        [JsonProperty]
        AssetInfo[] _assetInfos;

        public static AssetInfo[] AssetInfos {
            get => Instance._assetInfos ?? (Instance._assetInfos = Array.Empty<AssetInfo>());
            set => Instance._assetInfos = value;
        }

        private static ProjectCuratorData instance;
        public static ProjectCuratorData Instance {
            get {
                if (instance == null) {
                    if (File.Exists(JSON_PATH)) {
                        var json = File.ReadAllText(JSON_PATH);
                        instance = JsonConvert.DeserializeObject<ProjectCuratorData>(json, GuidConverter);
                    } else {
                        instance = new ProjectCuratorData();
                    }
                }
                return instance;
            }
        }

        private static readonly GuidConverter GuidConverter = new();
        public static void Save()
        {
            var json = JsonConvert.SerializeObject(Instance, GuidConverter);
            File.WriteAllText(JSON_PATH, json);
        }
    }
}