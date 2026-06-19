# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Renamed the authoring components to drop the `Component` suffix so the public
  type names read naturally in the inspector and in code: `RapierWorldComponent`
  → `RapierWorldBehaviour`, `RapierRigidBodyComponent` → `RapierRigidbody`,
  `RapierColliderComponent` → `RapierCollider`, `RapierJointComponent` →
  `RapierJoint` (and each `*JointComponent` → `*Joint`),
  `RapierCharacterControllerComponent` → `RapierCharacterControllerBehaviour`,
  and `RapierPidControllerComponent` → `RapierPidController`. The low-level
  struct `RapierCharacterController` keeps its name, so the component uses the
  `Behaviour` suffix to avoid a clash. Script `.meta` GUIDs are preserved, so
  existing Scenes/Prefabs keep their component references. This is a breaking
  change for code that referenced the old type names.
- Every concrete component now declares an `[AddComponentMenu]` entry, grouping
  them under **Rapier ▸ …** (with `Colliders`, `Joints`, and `Controllers`
  sub-menus) in the Add Component browser.

### Added
- Custom Inspectors for the authoring components, reorganized to mirror Unity's
  built-in physics components and lower the learning curve. The values users
  actually tune are shown up top; internal plumbing (transform sync, StableId /
  registration, lifecycle, deterministic-order tuning) is collapsed into an
  **Advanced** foldout. Highlights: Rigidbody shows a Unity-style **Constraints**
  grid and a Discrete/Continuous **Collision Detection** popup; colliders expose
  shape size the Unity way (Box **Size**, Capsule **Radius/Height**), **Is
  Trigger**, and a **Material** group; the character controller maps to **Slope
  Limit** / **Skin Width** with grouped Auto Step and Snap To Ground; joints use
  the Unity **Connected Body** model (**Anchor** / **Connected Anchor**) with the
  joint's own body auto-resolved; and collision/solver groups are edited as
  **LayerMask-style** 16-group dropdowns instead of raw integers. Body/world
  references auto-resolve when unambiguous. These are editor-only presentation
  changes — no serialized data or runtime behavior is affected.
- Data-driven scene import (Scene Sync foundation). `RapierSceneImporter` builds a
  `RapierWorldBehaviour` + body/collider GameObjects from a neutral, serializable
  `RapierSceneDescription` (also loadable from JSON), assigning StableId and
  RegistrationOrder and constructing the world in one deterministic
  `RebuildWorld()` pass. A `RapierImportedObject` metadata component records
  source-system/id/order, keeping the core components importer-agnostic. New
  **Scene Import** sample demonstrates the path. A downstream Scene Sync adapter
  maps its own format into `RapierSceneDescription`; this package stays Scene
  Sync-agnostic.
- `docs/component-api-coverage.md`: an explicit low-level ↔ component API mapping
  table (every `RapierWorld` call → its component-layer equivalent, or a note that
  it needs the low-level world), so component-layer gaps are tracked precisely.
- Stable id auto-generation. Rigid body, collider, and joint components gained an
  opt-in `AutoGenerateStableId` flag: when enabled and no id is set, a
  deterministic id is derived from the GameObject's hierarchy path (via the new
  `RapierStableId` helper) at registration, so the same hierarchy maps to the same
  id on every host without serialization. A `RapierStableId.Generate()` helper and
  a **Tools ▸ Rapier ▸ Assign Stable Ids To Selection** editor menu assign
  persistent ids into a Scene/Prefab. Auto-generation defaults off so existing
  state hashes are unchanged.
- Deterministic registration order for component worlds. `RapierWorldBehaviour`
  gained a `RegistrationMode` (`HierarchyOrder` / `StableId` / `ExplicitOrder`)
  and a `RebuildWorld()` method that discards and recreates the native world,
  creating every body, then collider, then joint in a fully determined order
  (independent of Unity's incidental component discovery). Rigid bodies,
  colliders, and joints gained a `RegistrationOrder` (and joints a `StableId`);
  joint limits/motors are cached and reapplied so joints survive a rebuild, and
  `RapierPidController` re-creates its controller when the world is
  rebuilt. The Rapier Component Demos sample exposes a registration-mode selector
  and a "Rebuild world" button. This is the foundation for Scene Sync import and
  network-bridge parity.
- Component layer adds dedicated authoring components for the advanced
  mesh/heightfield/voxel shapes used by the Rapier JS demos:
  `RapierConvexHullCollider`, `RapierTrimeshCollider`,
  `RapierHeightfieldCollider`, and `RapierVoxelsCollider` (each accepting code or
  serialized inline/mesh geometry). These join the existing box/sphere/capsule
  collider components.
- `RapierPidController` wraps the native PID controller for a target
  `RapierRigidbody`.
- `RapierRigidbody` gained authored body settings (stable id, gravity
  scale, soft-CCD prediction, additional solver iterations, dominance group,
  per-axis translation/rotation locks) plus `TryGetMass` / `SetSoftCcdPrediction`
  / `TryGetTransform` accessors.
- `RapierCollider` gained authored material/filter settings (stable id,
  friction/restitution combine rules, collision and solver groups, active events,
  active collision types, contact-force threshold) applied on creation.
- `RapierPhysics` query façade expanded to the full scene-query surface
  (filtered/all raycasts, point projection/intersection, shape cast/intersection)
  for both `RapierWorld` and `RapierWorldBehaviour`.
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
