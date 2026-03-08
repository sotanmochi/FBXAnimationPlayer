using System.Collections.Generic;
using UnityEngine;

namespace FbxAnimationPlayer
{
    /// <summary>
    /// Configuration for bone name mapping.
    /// Defines prefixes to strip and bone name patterns for matching.
    /// <br/>
    /// ボーン名マッピングの設定。
    /// 除去するプレフィックスとマッチングに使用するボーン名パターンを定義する。
    /// </summary>
    public class BoneNameMappingConfig
    {
        /// <summary>
        /// How this config interacts with the built-in default patterns.
        /// Defaults to Additive.
        /// <br/>
        /// このコンフィグが組み込みのデフォルトパターンに対してどのように作用するかを指定する。
        /// デフォルトはAdditive。
        /// </summary>
        public BoneNameMappingMode Mode = BoneNameMappingMode.Additive;

        /// <summary>
        /// Prefixes to strip from bone names during normalization (e.g., "mixamorig:", "character1_").
        /// Prefixes should include their delimiter to avoid false positives.
        /// <br/>
        /// 正規化時にボーン名から除去するプレフィックス（例: "mixamorig:", "character1_"）。
        /// 誤マッチ防止のため、区切り文字を含めること。
        /// </summary>
        public string[] PrefixesToStrip;

        /// <summary>
        /// Bone name patterns for matching. Key is HumanBodyBones, value is an array of normalized name patterns.
        /// Patterns should be lowercase with separator characters preserved (e.g., "hand_l", "upperarm_r").
        /// <br/>
        /// マッチングに使用するボーン名パターン。キーはHumanBodyBones、値は正規化済み名前パターンの配列。
        /// パターンは小文字で区切り文字を保持すること（例: "hand_l", "upperarm_r"）。
        /// </summary>
        public Dictionary<HumanBodyBones, string[]> BoneNamePatterns;
    }

    /// <summary>
    /// Specifies how a BoneNameMappingConfig is applied relative to the built-in default patterns.
    /// BoneNameMappingConfigをデフォルトパターンに対してどのように適用するかを指定する。
    /// </summary>
    public enum BoneNameMappingMode
    {
        /// <summary>
        /// The config is merged on top of the default patterns.
        /// Config prefixes are prepended to default prefixes.
        /// Config patterns for each bone are prepended to the corresponding default patterns,
        /// so config-defined patterns take priority.
        /// <br/>
        /// コンフィグをデフォルトパターンに追加でマージする。
        /// コンフィグのプレフィックスはデフォルトの先頭に追加される。
        /// 各ボーンのコンフィグパターンはデフォルトパターンの先頭に追加され、コンフィグ側が優先される。
        /// </summary>
        Additive,

        /// <summary>
        /// The config completely replaces all default prefixes and bone name patterns.
        /// コンフィグがデフォルトのプレフィックスとボーン名パターンをすべて置き換える。
        /// </summary>
        Override,
    }
}
