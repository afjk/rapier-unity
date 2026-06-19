# Rapier for Unity

[![Latest release](https://img.shields.io/github/v/release/afjk/rapier-unity?sort=semver)](https://github.com/afjk/rapier-unity/releases/latest)
[![Native CI](https://github.com/afjk/rapier-unity/actions/workflows/native.yml/badge.svg)](https://github.com/afjk/rapier-unity/actions/workflows/native.yml)
[![License](https://img.shields.io/badge/license-MIT%20OR%20Apache--2.0-blue)](#license)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com)

Rapier for Unity is a general-purpose Unity integration for the Rapier physics engine.

This project is maintained as part of the Scene Sync ecosystem, but is designed for any Unity project that needs deterministic, portable, replayable, or server-validatable physics.

This is not an official Rapier project.

## Getting Started

Install via Unity Package Manager (**+ → Add package from git URL…**):

```text
https://github.com/afjk/rapier-unity.git?path=Packages/com.afjk.rapier#v0.2.0
```

Prebuilt native plugins for **Windows, Linux, macOS (arm64), and Android
(arm64-v8a)** ship in the package, so no local Rust build is required on those
targets.

New here? See the [Getting Started guide](docs/getting-started.md) for the full
install options, a low-level API walkthrough, and the component API.

## Goals

- Provide explicit Rapier world ownership from Unity and C#.
- Support multiple independent physics worlds from the beginning.
- Expose a small, stable, versionable native FFI.
- Offer both a low-level C# API and Unity-friendly component API.
- Prefer deterministic fixed-step simulation and APIs that can support replay, rollback, snapshots, and state hashes.
- Keep Scene Sync integration downstream of this package.

## Non-goals

- This package does not replace Unity Physics globally.
- This package does not disable or modify Unity project physics settings.
- This package does not scan every Unity `Rigidbody` or `Collider` automatically.
- This package does not force Scene Sync concepts into the public API.
- This package is not an official Rapier distribution.

## Current Status

The package is in early foundation work. The initial native API covers explicit world creation, stepping, rigid bodies, primitive colliders, raycasts, deterministic state hashing, stable ids, and native snapshot restore. Unity components are intentionally opt-in and operate only on selected `RapierWorldComponent` instances.

## Architecture

The native library uses Rapier through a small C ABI. Every native call receives an explicit `world_id`, and the FFI layer keeps an internal registry of worlds:

```text
Unity C# RapierWorld
  -> DllImport C ABI
    -> native world registry
      -> RapierUnityWorld
        -> rapier3d PhysicsPipeline, RigidBodySet, ColliderSet, phases, joints
```

The registry is an implementation detail of the FFI layer. Public ownership remains explicit: Unity code owns a `RapierWorld`, and component worlds are owned by `RapierWorldComponent`.

## Low-level API

```csharp
using AFJK.Rapier;
using UnityEngine;

using var world = RapierWorld.Create();
world.SetGravity(new Vector3(0, -9.81f, 0));
world.SetTimestep(1f / 60f);

var body = world.CreateRigidBody(new RapierBodyDesc
{
    BodyType = RapierRigidBodyType.Dynamic,
    Position = Vector3.zero,
    Rotation = Quaternion.identity
});

world.CreateBoxCollider(body, new RapierBoxColliderDesc
{
    HalfExtents = Vector3.one * 0.5f,
    Density = 1f
});

world.Step();

if (world.TryGetTransform(body, out var transform))
{
    unityTransform.SetPositionAndRotation(transform.Position, transform.Rotation);
}
```

## Component API

The component API is opt-in:

- `RapierWorldComponent` owns one native `RapierWorld`.
- It can step automatically in `FixedUpdate` or be stepped manually.
- `RapierRigidBodyComponent` registers into a selected world.
- `RapierColliderComponent` implementations attach to a selected rigid body.
- Transform synchronization is explicit and configurable.
- Unity `Rigidbody` and built-in collider behavior is not changed.
- `RapierWorldComponent.RebuildWorld()` rebuilds the world with a deterministic
  registration order (`HierarchyOrder`, `StableId`, or `ExplicitOrder`), so the
  same scene/prefab produces the same body/collider/joint creation order on every
  host — the basis for Scene Sync import and deterministic network parity.
- Stable ids can be auto-generated: enable `AutoGenerateStableId` for a
  deterministic hierarchy-derived id at runtime, or use **Tools ▸ Rapier ▸ Assign
  Stable Ids To Selection** to bake persistent ids into a Scene/Prefab.

The component layer includes box, sphere, capsule, convex hull, trimesh,
heightfield, and voxel colliders, joints, a character controller, a PID
controller, and scene queries (via the `RapierPhysics` façade). The **Rapier
Component Demos** sample reimplements the current Rapier JS 3D demo catalog using
only these components — every body is a GameObject with Rapier components — which
serves as a practical coverage check for the component layer (it validates that
the demos are expressible as components, not that every Rapier API is wrapped;
see [docs/api-coverage.md](docs/api-coverage.md) for the per-API matrix). The
**Rapier JS Demos** sample builds the same scenes with the low-level API for
comparison.

## Native Build

Build the native library from the Rust workspace:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

The produced library is named `rapier_unity_ffi` with the platform-specific extension:

- macOS: `librapier_unity_ffi.dylib`
- Windows: `rapier_unity_ffi.dll`
- Linux: `librapier_unity_ffi.so`
- iOS/WebGL player builds: linked as `__Internal`

See [native/README.md](native/README.md) and [docs/native-packaging.md](docs/native-packaging.md) for platform notes.

## Parity profile and versioning

The current parity profile is **SceneSyncRapierParity-0.30**. This project is intentionally pinned to Rapier 0.30.0 for Browser/Unity bit parity. API expansion should target rapier.js 0.19.3 compatibility first. Latest Rapier support may be added later as a separate profile once the current parity baseline is stable.

See [docs/support-matrix.md](docs/support-matrix.md) for platform and backend support status, and [docs/api-coverage.md](docs/api-coverage.md) for the Rapier JS API coverage matrix and recommended implementation order.

## Roadmap

- Foundation: repository structure, package metadata, native workspace, low-level world wrapper.
- Rigid bodies and primitive colliders.
- Unity component API.
- Scene queries and deterministic replay samples.
- Native packaging/artifact CI.
- More platforms, joints, character controller, events, and rollback tooling.

Snapshot format notes live in [docs/snapshot-design.md](docs/snapshot-design.md).
Scene Sync parity notes live in [docs/scenesync-parity.md](docs/scenesync-parity.md).
Platform and backend support live in [docs/support-matrix.md](docs/support-matrix.md).
API coverage and implementation order live in [docs/api-coverage.md](docs/api-coverage.md).
The low-level ↔ component API mapping lives in [docs/component-api-coverage.md](docs/component-api-coverage.md).

## Relationship to Scene Sync

Scene Sync can use this package as a downstream consumer. Core APIs must stay general-purpose and should make sense to Unity developers who are not using Scene Sync.

## Relationship to Rapier

This package integrates the Rapier physics engine into Unity through a native Rust plugin. It is not maintained by the Rapier project and should not be presented as an official Rapier package.

## License

Licensed under either of:

- MIT, see [LICENSE-MIT](LICENSE-MIT)
- Apache-2.0, see [LICENSE-APACHE](LICENSE-APACHE)

at your option.
