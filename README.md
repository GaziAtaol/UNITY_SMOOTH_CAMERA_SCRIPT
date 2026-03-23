# Unity Smooth Camera Follow

A lightweight, production-ready third-person camera script for Unity that features smooth position/rotation following, network lag compensation, and automatic obstacle avoidance. Works in both **single-player** and **multiplayer** (Unity Netcode for GameObjects) projects.

---

## Features

| Feature | Description |
|---|---|
| **Smooth follow** | `SmoothDamp`-based position interpolation — no jarring cuts |
| **Smooth rotation** | `Slerp`-based rotation — follows the target's forward or looks directly at it |
| **Collision detection** | `SphereCast`-based obstacle avoidance — the camera moves closer when walls are in the way |
| **Network lag prediction** | Extrapolates the target's next position to hide latency jitter in multiplayer |
| **Multiplayer-ready** | Extends `NetworkBehaviour`; only the owning client runs the camera |
| **Single-player fallback** | Automatically initialises when no `NetworkManager` is present |
| **Gizmo preview** | Visualises the camera offset and collision sphere in the Scene view |

---

## Requirements

- **Unity** 2021.3 LTS or newer (tested on 2022.3 and 6000.x)
- **Unity Netcode for GameObjects** (`com.unity.netcode.gameobjects`) — only required for multiplayer use. If you are building a single-player game the script still works; just ignore the `NetworkBehaviour` base class warning if Netcode is not installed.

---

## Quick Start

### 1 — Add the script

Copy `SmoothCameraFollow.cs` into any `Assets/Scripts` folder in your project.

### 2 — Set up the hierarchy

```
Player (NetworkObject)
└── CameraRoot          ← empty GameObject, rotated by your look/orbit logic
    └── Main Camera     ← attach SmoothCameraFollow here
```

Alternatively the camera can be a **standalone GameObject** (not parented to the player) — just assign the `Target` field manually.

### 3 — Configure the Inspector

Select the **Main Camera** and adjust the fields described in the [Inspector Reference](#inspector-reference) section below.

### 4 — Run

Press **Play**. The camera will smoothly follow the target, avoid walls, and (in multiplayer) compensate for network lag.

---

## Inspector Reference

### Follow Settings

| Field | Type | Default | Description |
|---|---|---|---|
| **Target** | `Transform` | `null` | The transform to follow. Leave empty to auto-assign to the parent GameObject. |
| **Offset** | `Vector3` | `(0, 1.8, -3.5)` | Camera position relative to the target's local space. Adjust X/Y/Z to reposition the camera. |
| **Look At Target** | `bool` | `false` | When **enabled**, the camera rotates to face the target directly. When **disabled**, it mirrors the target's forward direction. |

### Smooth Settings

| Field | Type | Default | Description |
|---|---|---|---|
| **Position Smooth Speed** | `float` | `10` | Controls how quickly the camera catches up to the desired position. Higher = snappier. Minimum `0.1`. |
| **Rotation Smooth Speed** | `float` | `8` | Controls rotation interpolation speed. Set to `0` to disable rotation smoothing entirely. |

### Collision Detection

| Field | Type | Default | Description |
|---|---|---|---|
| **Use Collision Detection** | `bool` | `true` | Enables obstacle avoidance via a sphere-cast between the target and the desired camera position. |
| **Collision Layers** | `LayerMask` | All | Which physics layers the sphere-cast tests against. Exclude the player's own layer to avoid self-collision. |
| **Collision Radius** | `float` | `0.2` | Radius of the sphere used in the cast. Increase to keep the camera further from walls. |
| **Min Distance** | `float` | `0.5` | Minimum distance the camera can be pushed to when clipping is detected. |

### Prediction (Multiplayer)

| Field | Type | Default | Description |
|---|---|---|---|
| **Use Position Prediction** | `bool` | `true` | Extrapolates the target's next position based on last-frame movement to smooth out network lag. |
| **Prediction Multiplier** | `float` | `1.5` | How far ahead to extrapolate. `1.0` = one frame ahead. Increase if lag is high; decrease if it causes jitter. |

---

## Usage Examples

### Single-Player (no Netcode)

Simply attach the script to your camera. No extra setup is required — the `Start()` fallback initialises the camera automatically.

```csharp
// No code needed — configure everything in the Inspector.
```

### Multiplayer (Unity Netcode for GameObjects)

1. Ensure the **Player** prefab has a `NetworkObject` component.
2. Attach `SmoothCameraFollow` to the camera that is a child of the player prefab.
3. The script automatically disables itself on non-owning clients so only the local player drives its own camera.

```csharp
// The script handles IsOwner checks internally — nothing extra needed.
```

### Changing the Target at Runtime

The script does not expose a public target setter by default. To support runtime target switching, add the following method to the class:

```csharp
public void SetTarget(Transform newTarget)
{
    target = newTarget;
    if (target != null)
        _lastTargetPosition = target.position;
}
```

---

## Tips

- **Exclude the player layer** from `Collision Layers` to prevent the sphere-cast from hitting the player's own collider.
- **Offset tuning**: Use the **Scene Gizmos** (yellow sphere + line) to preview the camera position without entering Play Mode.
- **First-person**: Set `Offset` to `(0, 0, 0)` and `Look At Target` to `false`. The camera will sit exactly at the target and face its forward direction.
- **Top-down**: Set `Offset` to `(0, 10, 0)`, enable `Look At Target` so the camera always faces down toward the player.
- **High-latency servers**: Increase `Prediction Multiplier` to `2.0`–`3.0` to further compensate for lag. If you see overshoot, reduce it back toward `1.0`.

---

## License

MIT — see [LICENSE](LICENSE) for details.
