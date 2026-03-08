using System;
using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    /// <summary>
    /// Provides JSON serialization and deserialization for <see cref="BoneNameMappingConfig"/>.
    /// Uses JsonUtility internally via <see cref="SerializedBoneNameMappingConfig"/>.
    /// <br/>
    /// BoneNameMappingConfigのJSONシリアライズ・デシリアライズを提供する。
    /// 内部でJsonUtilityをSerializedBoneNameMappingConfig経由で使用する。
    /// </summary>
    public static class BoneNameMappingConfigJsonSerializer
    {
        public static BoneNameMappingConfig Deserialize(string json)
        {
            var raw = JsonUtility.FromJson<SerializedBoneNameMappingConfig>(json);
            return raw.ToConfig();
        }

        public static string Serialize(BoneNameMappingConfig config, bool prettyPrint = true)
        {
            var raw = SerializedBoneNameMappingConfig.FromConfig(config);
            return JsonUtility.ToJson(raw, prettyPrint);
        }
    }

    /// <summary>
    /// Serializable representation of BoneNameMappingConfig for JsonUtility.
    /// JsonUtility does not support Dictionary, so this class uses arrays of key-value pairs.
    /// <br/>
    /// JsonUtility用のBoneNameMappingConfigのシリアライズ可能な表現。
    /// JsonUtilityはDictionaryをサポートしないため、キー・バリューペアの配列を使用する。
    /// </summary>
    [Serializable]
    internal class SerializedBoneNameMappingConfig
    {
        public string mode;
        public string[] prefixesToStrip;
        public BoneEntry[] bones;

        [Serializable]
        internal class BoneEntry
        {
            public string bone;
            public string[] patterns;
        }

        internal BoneNameMappingConfig ToConfig()
        {
            var parsedMode = BoneNameMappingMode.Additive;
            if (!string.IsNullOrEmpty(mode) && Enum.TryParse<BoneNameMappingMode>(mode, out var m))
            {
                parsedMode = m;
            }

            var config = new BoneNameMappingConfig
            {
                Mode = parsedMode,
                PrefixesToStrip = prefixesToStrip,
            };

            if (bones != null)
            {
                config.BoneNamePatterns = new Dictionary<HumanBodyBones, string[]>();
                foreach (var entry in bones)
                {
                    if (Enum.TryParse<HumanBodyBones>(entry.bone, out var boneEnum))
                    {
                        config.BoneNamePatterns[boneEnum] = entry.patterns;
                    }
                    else
                    {
                        Debug.LogWarning($"[BoneNameMappingConfig] Unknown bone name: '{entry.bone}'");
                    }
                }
            }

            return config;
        }

        internal static SerializedBoneNameMappingConfig FromConfig(BoneNameMappingConfig config)
        {
            var raw = new SerializedBoneNameMappingConfig
            {
                mode = config.Mode.ToString(),
                prefixesToStrip = config.PrefixesToStrip,
            };

            if (config.BoneNamePatterns != null)
            {
                var entries = new List<BoneEntry>();
                foreach (var (bone, patterns) in config.BoneNamePatterns)
                {
                    entries.Add(new BoneEntry { bone = bone.ToString(), patterns = patterns });
                }
                raw.bones = entries.ToArray();
            }

            return raw;
        }
    }
}
