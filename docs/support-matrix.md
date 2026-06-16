# Support Matrix

This document tracks Rapier for Unity support across two axes: **platform support** and **backend support**.

Support status uses these labels:

- **Supported** — working and validated
- **Planned** — committed future work
- **Manual/local** — works but requires manual steps; no CI validation
- **Not implemented** — no work started yet

## Platform Matrix

| Platform | Backend | Current status | Target status | Notes |
|---|---|---|---|---|
| Unity Editor macOS | Native Rust FFI | Manual/local | Supported | Primary development platform |
| Unity Editor Windows | Native Rust FFI | Planned/manual | Supported | Requires native DLL packaging |
| Unity Editor Linux | Native Rust FFI | Native CI only | Supported | Requires Unity validation |
| macOS Standalone | Native Rust FFI | Planned | Supported | Needs plugin import settings and packaging |
| Windows Standalone | Native Rust FFI | Planned | Supported | Needs DLL packaging |
| Linux Standalone | Native Rust FFI | Planned | Supported | Needs .so packaging |
| Android | Native Rust FFI | Not implemented | Planned | Needs NDK cross-build and ABI layout |
| iOS | Native Rust FFI staticlib | Not implemented | Planned | Needs static library, `__Internal`, Xcode integration |
| visionOS | Native Rust FFI staticlib | Not implemented | Planned | Similar to iOS but must be validated separately |
| Unity WebGL | rapier.js wasm backend | Not implemented | Planned | Should use JS/Wasm route, not native FFI |
| Browser Scene Sync | rapier.js wasm | Working parity target | Supported for Scene Sync | Uses deterministic-compat 0.19.3 |
| Godot | External integration | Not implemented here | Future parity target | Track same Rapier core/profile when needed |

## Backend Matrix

| Backend | Used by | Current status | Notes |
|---|---|---|---|
| Native Rust FFI | Unity Editor / Standalone / Mobile / XR | Current primary backend | C ABI into rapier3d |
| rapier.js wasm | Browser Scene Sync | Current browser backend | deterministic-compat 0.19.3 |
| Unity WebGL JS/Wasm backend | Unity WebGL | Planned | Should bridge from C# to JS plugin / rapier.js |
| Native snapshot | Unity native | Implemented for same FFI/core profile | Not cross-host canonical snapshot |
| Canonical snapshot | Browser / Unity / Godot | Not implemented | Future versioned cross-host schema |

## Physics Profiles

Support is versioned by physics profile. A profile pins the Rapier core version, the browser package, and the Unity native crate together. Cross-host parity results are only meaningful within the same profile.

| Profile | Rapier core | Browser package | Unity native crate | Purpose |
|---|---|---|---|---|
| SceneSyncRapierParity-0.30 | 0.30.0 | `@dimforge/rapier3d-deterministic-compat@0.19.3` | `rapier3d = 0.30.0` + enhanced-determinism | Browser/Unity bit parity |
| LatestNative | latest Rapier | none yet | future/latest rapier3d | Unity-only latest API exploration, no parity guarantee |

### Profile rules

- **SceneSyncRapierParity-0.30** is the current compatibility target. All parity fixtures and cross-host hash comparisons use this profile.
- **LatestNative** is not implemented yet.
- Do not mix profiles when comparing hashes or snapshots.
- Upgrading Rapier requires updating the browser package and the Unity native crate together, then regenerating parity fixtures and re-validating canonical hashes.

See [scenesync-parity.md](scenesync-parity.md) for the hash scheme, fixture format, and cross-host snapshot policy.
