# Fbx Animation Player

A runtime FBX animation importer and player for Unity.

## Dependencies
- [UniTask](https://github.com/Cysharp/UniTask)
- [Lightweight FBX Importer](https://ricardoreis.net/lightweight-fbx-importer/)
    - [Unity Asset Store](https://assetstore.unity.com/packages/tools/modeling/lightweight-fbx-importer-318963)
    - [Documentation](https://ricardoreis.net/fbximporter/docs/)

## Installation
You can install via Package Manager in UnityEditor.

1. Open the Package Manager window
2. Click the + button and select "Add package from git URL"
3. Enter: `https://github.com/sotanmochi/FBXAnimationPlayer.git?path=src/FBXAnimationPlayer/Assets/FbxAnimationPlayer#0.2.0`

<img src="./Docs~/PackageManager.png" height="480">

You can also install via editing Packages/manifest.json directly.
```
// Packages/manifest.json
{
  "dependencies": {
    ...
    "jp.sotanmochi.fbxanimationplayer": "https://github.com/sotanmochi/FBXAnimationPlayer.git?path=src/FBXAnimationPlayer/Assets/FbxAnimationPlayer#0.2.0",
    ...
  }
}
```

### Lightweight FBX Importer
- [Lightweight FBX Importer Setup (English)](./Docs~/setup-lightweight-fbx-importer-en.md)
- [Lightweight FBX Importer のセットアップ (日本語)](./Docs~/setup-lightweight-fbx-importer-ja.md)
