# Rapier for Unity

Rapier for Unity is a general-purpose Unity integration for the Rapier physics engine.

This project is maintained as part of the Scene Sync ecosystem, but is designed for any Unity project that needs deterministic, portable, replayable, or server-validatable physics.

This is not an official Rapier project.

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

The package is in early foundation work. The initial native API covers explicit world creation, stepping, rigid bodies, primitive colliders, raycasts, deterministic state hashing, and snapshot API stubs. Unity components are intentionally opt-in and operate only on selected `RapierWorldComponent` instances.

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

## Roadmap

- Foundation: repository structure, package metadata, native workspace, low-level world wrapper.
- Rigid bodies and primitive colliders.
- Unity component API.
- Scene queries and deterministic replay samples.
- Snapshot/restore implementation and native packaging/artifact CI.
- More platforms, joints, character controller, events, and rollback tooling.

Snapshot format notes live in [docs/snapshot-design.md](docs/snapshot-design.md).

## Relationship to Scene Sync

Scene Sync can use this package as a downstream consumer. Core APIs must stay general-purpose and should make sense to Unity developers who are not using Scene Sync.

## Relationship to Rapier

This package integrates the Rapier physics engine into Unity through a native Rust plugin. It is not maintained by the Rapier project and should not be presented as an official Rapier package.

## License

Licensed under either of:

- MIT, see [LICENSE-MIT](LICENSE-MIT)
- Apache-2.0, see [LICENSE-APACHE](LICENSE-APACHE)

at your option.
