# Fbx Animation Player

A runtime FBX animation importer and player for Unity.

## Dependencies
- [Lightweight FBX Importer](https://ricardoreis.net/lightweight-fbx-importer/)
    - [Unity Asset Store](https://assetstore.unity.com/packages/tools/modeling/lightweight-fbx-importer-318963)
    - [Documentation](https://ricardoreis.net/fbximporter/docs/)

## Installation
You can install via Package Manager in UnityEditor.

1. Open the Package Manager window
2. Click the + button and select "Add package from git URL"
3. Enter: `https://github.com/sotanmochi/FBXAnimationPlayer.git?path=src/FBXAnimationPlayer/Assets/FBXAnimationPlayer#0.1.0`

You can also install via editing Packages/manifest.json directly.
```
// Packages/manifest.json
{
  "dependencies": {
    ...
    "jp.sotanmochi.fbxanimationplayer": "https://github.com/sotanmochi/FBXAnimationPlayer.git?path=src/FBXAnimationPlayer/Assets/FBXAnimationPlayer#0.1.0",
    ...
  }
}
```
