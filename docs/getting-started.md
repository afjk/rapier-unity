# Getting Started

This guide walks through installing **Rapier for Unity** and running your first
simulation with both the low-level API and the opt-in component API.

## Requirements

- Unity **2022.3** or newer.
- A platform with a bundled native plugin: **Windows (x86_64)**, **Linux
  (x86_64)**, **macOS (Apple Silicon / arm64)**, or **Android (arm64-v8a)**.
  Other targets require a local native build (see [Native plugin](#native-plugin)).

## Install

### Option A — Unity Package Manager, Git URL (recommended)

1. Open **Window → Package Manager**.
2. Click **+ → Add package from git URL…**
3. Enter:

   ```text
   https://github.com/afjk/rapier-unity.git?path=Packages/com.afjk.rapier
   ```

To pin a specific release, append a tag:

```text
https://github.com/afjk/rapier-unity.git?path=Packages/com.afjk.rapier#v0.1.0
```

### Option B — manifest.json

Add the dependency to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.afjk.rapier": "https://github.com/afjk/rapier-unity.git?path=Packages/com.afjk.rapier#v0.1.0"
  }
}
```

### Option C — local clone

Clone the repository and add the package from disk in Package Manager
(**+ → Add package from disk…**), selecting:

```text
Packages/com.afjk.rapier/package.json
```

### Native plugin

Prebuilt native plugins for Windows, Linux, macOS (arm64), and Android
(arm64-v8a) ship inside the package under `Runtime/Plugins`, so no extra setup
is needed on those targets.

For an unsupported target (for example an Intel mac, or another Android ABI)
build the crate and copy the platform library into the matching plugin folder:

```sh
cd native
cargo build --release -p rapier_unity_ffi
# Android: cargo ndk -t arm64-v8a -o ./dist build --release -p rapier_unity_ffi
```

| Platform | Output file | Plugin folder |
| --- | --- | --- |
| Windows | `rapier_unity_ffi.dll` | `Runtime/Plugins/Windows` |
| Linux | `librapier_unity_ffi.so` | `Runtime/Plugins/Linux` |
| macOS | `librapier_unity_ffi.dylib` | `Runtime/Plugins/macOS` |
| Android (arm64-v8a) | `librapier_unity_ffi.so` | `Runtime/Plugins/Android/arm64-v8a` |

## Quick start: low-level API

The low-level API gives you direct, explicit ownership of a Rapier world. A
world is `IDisposable` — dispose it when you are done. Step it yourself and read
transforms back to drive your visuals.

```csharp
using AFJK.Rapier;
using UnityEngine;

public sealed class FallingBox : MonoBehaviour
{
    private RapierWorld world;
    private RapierRigidBodyHandle body;

    private void Start()
    {
        world = RapierWorld.Create();
        world.SetGravity(new Vector3(0f, -9.81f, 0f));
        world.SetTimestep(1f / 60f);

        // Static floor.
        var floor = world.CreateRigidBody(new RapierBodyDesc
        {
            BodyType = RapierRigidBodyType.Fixed,
            Position = Vector3.zero,
            Rotation = Quaternion.identity
        });
        world.CreateBoxCollider(floor, new RapierBoxColliderDesc
        {
            HalfExtents = new Vector3(10f, 0.5f, 10f),
            Density = 1f
        });

        // Dynamic box that falls onto the floor.
        body = world.CreateRigidBody(new RapierBodyDesc
        {
            BodyType = RapierRigidBodyType.Dynamic,
            Position = new Vector3(0f, 5f, 0f),
            Rotation = Quaternion.identity
        });
        world.CreateBoxCollider(body, new RapierBoxColliderDesc
        {
            HalfExtents = Vector3.one * 0.5f,
            Density = 1f
        });
    }

    private void FixedUpdate()
    {
        world.Step();

        if (world.TryGetTransform(body, out var t))
        {
            transform.SetPositionAndRotation(t.Position, t.Rotation);
        }
    }

    private void OnDestroy()
    {
        world?.Dispose();
        world = null;
    }
}
```

> Stepping in `FixedUpdate` with a fixed timestep keeps the simulation
> deterministic, which is required for replay, rollback, and state hashing.

## Quick start: component API

The component API authors worlds and bodies directly in a scene. Components are
opt-in and never touch Unity's built-in `Rigidbody`/`Collider` components.

### Scene hierarchy

```text
RapierWorld          (GameObject)  + RapierWorldBehaviour
└── Box              (GameObject)  + RapierRigidbody
                                   + RapierBoxCollider
```

1. Create an empty GameObject named e.g. `RapierWorld` and add
   **`RapierWorldBehaviour`**. Leave **Step Mode** at `FixedUpdate` so it steps
   automatically; set **Gravity** and **Timestep** as needed.
2. Create a child GameObject and add **`RapierRigidbody`**. It resolves
   its world from the nearest parent `RapierWorldBehaviour` automatically (or
   assign one explicitly in the inspector). Set **Body Type** (`Dynamic`,
   `Fixed`, `KinematicPositionBased`, `KinematicVelocityBased`).
3. On the same GameObject, add a collider component — one of
   **`RapierBoxCollider`**, **`RapierSphereCollider`**, **`RapierCapsuleCollider`**,
   or **`RapierMeshCollider`**. It attaches to the rigid body found on itself or
   a parent.
4. Enter Play Mode. With `syncTransformFromRapier` enabled (default), the body's
   GameObject transform follows the simulation each step.

### Building the same hierarchy from code

```csharp
using AFJK.Rapier;
using UnityEngine;

public sealed class ComponentBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var worldGo = new GameObject("RapierWorld");
        worldGo.AddComponent<RapierWorldBehaviour>(); // steps in FixedUpdate

        var boxGo = new GameObject("Box");
        boxGo.transform.SetParent(worldGo.transform);
        boxGo.transform.position = new Vector3(0f, 5f, 0f);

        var bodyComponent = boxGo.AddComponent<RapierRigidbody>();
        bodyComponent.BodyType = RapierRigidBodyType.Dynamic;

        var box = boxGo.AddComponent<RapierBoxCollider>();
        box.HalfExtents = Vector3.one * 0.5f;
    }
}
```

### Manual stepping

To drive the world yourself, set **Step Mode** to `Manual` (or
`worldComponent.StepMode = RapierWorldStepMode.Manual`) and call `Step()`:

```csharp
worldComponent.Step();           // syncs bodies, steps once
var hash = worldComponent.StateHash();
```

## Determinism, snapshots, and events

- `RapierWorld.StateHash()` / `RapierWorldBehaviour.StateHash()` return a
  canonical hash for parity checks and replay validation.
- `TryCreateSnapshot` / `TryReadSnapshot` save and restore full world state for
  rollback.
- `DrainCollisionEvents` / `DrainContactForceEvents` read events produced by the
  last step (enable the relevant active events on the collider first).

See [scenesync-parity.md](scenesync-parity.md) and
[snapshot-design.md](snapshot-design.md) for details.

## Samples

Import these from Package Manager (**Samples** tab) and open their scenes:

- **Basic Falling Ball** — minimal component-API scene.
- **Deterministic Replay** — compares two worlds by state hash.
- **Cross-Host Parity** — runs a shared Unity/Browser fixture and logs canonical hashes.
- **Rapier JS Demos** — 17 ported demos from the Rapier 3D JS catalog.

## Next steps

- [API Coverage Matrix](api-coverage.md) — what is implemented and the planned order.
- [Support Matrix](support-matrix.md) — platform and backend status.
- [Native Packaging Notes](native-packaging.md) — how the native plugins are built and bundled.
