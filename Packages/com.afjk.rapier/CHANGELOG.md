# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
