# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Stable id auto-generation. Rigid body, collider, and joint components gained an
  opt-in `AutoGenerateStableId` flag: when enabled and no id is set, a
  deterministic id is derived from the GameObject's hierarchy path (via the new
  `RapierStableId` helper) at registration, so the same hierarchy maps to the same
  id on every host without serialization. A `RapierStableId.Generate()` helper and
  a **Tools ▸ Rapier ▸ Assign Stable Ids To Selection** editor menu assign
  persistent ids into a Scene/Prefab. Auto-generation defaults off so existing
  state hashes are unchanged.
- Deterministic registration order for component worlds. `RapierWorldComponent`
  gained a `RegistrationMode` (`HierarchyOrder` / `StableId` / `ExplicitOrder`)
  and a `RebuildWorld()` method that discards and recreates the native world,
  creating every body, then collider, then joint in a fully determined order
  (independent of Unity's incidental component discovery). Rigid bodies,
  colliders, and joints gained a `RegistrationOrder` (and joints a `StableId`);
  joint limits/motors are cached and reapplied so joints survive a rebuild, and
  `RapierPidControllerComponent` re-creates its controller when the world is
  rebuilt. The Rapier Component Demos sample exposes a registration-mode selector
  and a "Rebuild world" button. This is the foundation for Scene Sync import and
  network-bridge parity.
- Component layer adds dedicated authoring components for the advanced
  mesh/heightfield/voxel shapes used by the Rapier JS demos:
  `RapierConvexHullCollider`, `RapierTrimeshCollider`,
  `RapierHeightfieldCollider`, and `RapierVoxelsCollider` (each accepting code or
  serialized inline/mesh geometry). These join the existing box/sphere/capsule
  collider components.
- `RapierPidControllerComponent` wraps the native PID controller for a target
  `RapierRigidBodyComponent`.
- `RapierRigidBodyComponent` gained authored body settings (stable id, gravity
  scale, soft-CCD prediction, additional solver iterations, dominance group,
  per-axis translation/rotation locks) plus `TryGetMass` / `SetSoftCcdPrediction`
  / `TryGetTransform` accessors.
- `RapierColliderComponent` gained authored material/filter settings (stable id,
  friction/restitution combine rules, collision and solver groups, active events,
  active collision types, contact-force threshold) applied on creation.
- `RapierPhysics` query façade expanded to the full scene-query surface
  (filtered/all raycasts, point projection/intersection, shape cast/intersection)
  for both `RapierWorld` and `RapierWorldComponent`.
- New **Rapier Component Demos** sample: reimplements the current Rapier JS 3D
  demo catalog using the component layer (each body is a GameObject with Rapier
  components). This validates practical component coverage; it is not a proof of
  complete Rapier API coverage.

## [0.2.0] - 2026-06-18

### Added
- Android (arm64-v8a) native plugin, cross-built with cargo-ndk and bundled
  under `Runtime/Plugins/Android/arm64-v8a`. No C# change is required;
  `DllImport` already resolves the library on Android.
- Getting Started guide (`docs/getting-started.md`) covering UPM git-URL /
  manifest / local installation and both the low-level and component APIs.

### Changed
- Native CI now builds pull requests on Linux only and runs the full
  cross-platform matrix, Android build, and artifact uploads on `main` and
  manual dispatch, with cargo build caching for faster runs.

## [0.1.0] - 2026-06-18

Initial preview release of the foundation work.

### Added
- Explicit Rapier world ownership from Unity/C# with support for multiple
  independent physics worlds.
- Low-level `RapierWorld` API: world stepping, rigid bodies, primitive and mesh
  colliders (box, sphere, capsule, trimesh, convex hull, heightfield, voxels),
  forces/impulses, CCD, damping, dominance, and per-collider material properties.
- Scene queries: raycasts (single/all/filtered), shape casts, point projection,
  and shape intersection tests.
- Joints (fixed, spherical, revolute, prismatic, rope, spring) with motors and
  limits, a kinematic character controller, and collision/contact-force events.
- Deterministic helpers: state hashing, stable ids, and native snapshot
  save/restore for replay and rollback workflows.
- Opt-in Unity component API (`RapierWorldComponent`, rigid body, collider,
  joint, and character controller components).
- Samples: Basic Falling Ball, Deterministic Replay, Cross-Host Parity, and
  Rapier JS Demos (17 ported demos from the Rapier 3D JS catalog).
- Prebuilt native FFI plugins for Windows (x86_64), Linux (x86_64), and macOS
  (Apple Silicon / arm64).

### Known limitations
- The macOS plugin is arm64 only; Intel macs require a local build until a
  universal binary is shipped.
- Cone and cylinder native primitive colliders are not yet exposed (the samples
  approximate them with convex hulls).
- No automated Unity (C#) play-mode/edit-mode tests yet; the native FFI is
  covered by Rust unit tests run in CI on Linux, macOS, and Windows.
