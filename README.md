# Fbx Animation Player

[日本語版 README はこちら](./README_ja.md)

A runtime FBX animation importer and player for Unity.

## Demo
- https://sotanmochi.github.io/FBXAnimationPlayer/vrm-viewer-unityweb/

## Tested Environment
- Unity 6000.0.58f2

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

<img src="./src/FBXAnimationPlayer/Assets/FbxAnimationPlayer/Docs~/PackageManager.png" height="480">

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
- [Lightweight FBX Importer Setup (English)](./src/FBXAnimationPlayer/Assets/FbxAnimationPlayer/Docs~/setup-lightweight-fbx-importer-en.md)
- [Lightweight FBX Importer のセットアップ (日本語)](./src/FBXAnimationPlayer/Assets/FbxAnimationPlayer/Docs~/setup-lightweight-fbx-importer-ja.md)

## Basic Usage

### Loading an FBX File

You can load an FBX file using `FbxAnimationImporter.LoadAsync`.

```csharp:FbxAnimationSample.cs
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using FbxAnimationPlayer;
using UnityEngine;

public class FbxAnimationSample : MonoBehaviour
{
    async void Start()
    {
        var token = this.GetCancellationTokenOnDestroy();

        var filePath = "/path/to/animation.fbx";

        // Load from a file path
        var result = await FbxAnimationImporter.LoadAsync(filePath, token);

        // Load from a stream
        // using var stream = new FileStream(filePath, FileMode.Open);
        // var result = await FbxAnimationImporter.LoadAsync(stream, token);

        if (!result.IsSuccess)
        {
            Debug.LogError(result.ErrorMessage);
            return;
        }

        // Loading succeeded — start playback
        result.AnimationController.Play();
    }
}
```

### ImportResult Properties

`ImportResult` returned by `FbxAnimationImporter.LoadAsync` has three main properties.

| Property | Type | Description |
|---|---|---|
| `MotionActor` | `FbxMotionActor` | Retrieve HumanPose and apply it to other humanoid avatars |
| `AnimationController` | `FbxAnimationController` | Control playback, pause, stop, seek, etc. |
| `AnimationClips` | `IReadOnlyList<AnimationClip>` | Direct access to AnimationClips |

### Playback Control

You can control animation playback via `FbxAnimationController`.

```csharp
var ctrl = result.AnimationController;

// Basic playback control
ctrl.Play();                // Play (from the beginning if stopped, resume if paused)
ctrl.Pause();               // Pause
ctrl.Stop();                // Stop (rewind to the beginning)

// Seek
ctrl.Seek(2.5f);            // Seek to a specific time in seconds (transitions to paused state if called while stopped)
ctrl.SeekNormalized(0.5f);  // Seek by normalized time (0.0 – 1.0)

// Speed & loop
ctrl.Speed = 2.0f;          // 2x speed
ctrl.Speed = -1.0f;         // Reverse playback (negative values supported)
ctrl.IsLooping = false;     // Disable looping (stops at the end)

// State inspection
Debug.Log(ctrl.State);          // Stopped / Playing / Paused
Debug.Log(ctrl.CurrentTime);    // Current playback time (seconds)
Debug.Log(ctrl.Duration);       // Total clip duration (seconds)
Debug.Log(ctrl.NormalizedTime); // Normalized playback time (0.0 – 1.0)
```

You can also subscribe to events.

```csharp
ctrl.StateChanged += state => Debug.Log($"State changed: {state}");
ctrl.ClipFinished += () => Debug.Log("Playback finished");
ctrl.TimeUpdated += time => slider.value = ctrl.NormalizedTime;
```

### Applying Animation to Another Character

By passing the `HumanPose` obtained from `FbxMotionActor.TryGetHumanPose` to [`HumanPoseHandler.SetHumanPose`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/HumanPoseHandler.SetHumanPose.html), you can apply the animation to another character.

First, initialize a [`HumanPoseHandler`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/HumanPoseHandler.html). The target character that receives the `HumanPose` must have an `Animator` with a Humanoid Avatar.

```csharp:AvatarMotionReceiverSample.cs
using UnityEngine;

public class AvatarMotionReceiverSample : MonoBehaviour
{
    [SerializeField] private Animator _targetAnimator;

    private HumanPoseHandler _targetPoseHandler;

    void Start()
    {
        _targetPoseHandler = new HumanPoseHandler(
            _targetAnimator.avatar,
            _targetAnimator.transform
        );
    }

    void OnDestroy()
    {
        _targetPoseHandler?.Dispose();
    }
}
```

After loading the FBX file, retrieve the pose with `FbxMotionActor.TryGetHumanPose` and apply it with [`HumanPoseHandler.SetHumanPose`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/HumanPoseHandler.SetHumanPose.html).

```csharp:AvatarMotionReceiverSample.cs (continued)
private ImportResult _fbxResult;

public async void LoadAndApply(string filePath, CancellationToken token)
{
    _fbxResult = await FbxAnimationImporter.LoadAsync(filePath, token);
    if (!_fbxResult.IsSuccess || _fbxResult.MotionActor == null)
    {
        Debug.LogError(_fbxResult.ErrorMessage);
        return;
    }

    _fbxResult.AnimationController.Play();
}

void LateUpdate()
{
    if (_fbxResult == null || _fbxResult.MotionActor == null) return;

    var humanPose = new HumanPose();
    if (_fbxResult.MotionActor.TryGetHumanPose(ref humanPose))
    {
        _targetPoseHandler.SetHumanPose(ref humanPose);
    }
}
```
