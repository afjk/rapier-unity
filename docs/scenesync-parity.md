# Scene Sync Parity Profile

This profile defines the minimum contract for comparing Rapier physics state
across Scene Sync hosts such as Unity, Browser, and Godot.

Web/Unity parity depends on matching the selected physics profile. See
[support-matrix.md](support-matrix.md) for the full list of profiles, platform
support status, and backend strategy.

Unity WebGL should eventually use a rapier.js / Wasm backend rather than the
native FFI path. Until that backend exists, Unity WebGL is not a valid parity
host.

## Target

- Rapier Rust core: `0.30.0`
- Unity FFI crate: `rapier3d = "=0.30.0"`
- Unity FFI features: `enhanced-determinism`, `serde-serialize`
- Browser package: `@dimforge/rapier3d-deterministic-compat@0.19.3`
- Browser Rapier core target: `0.30.0`
- Hash scheme: `SceneSyncCanonicalPhysicsHashV1`

Do not compare parity results across different Rapier core versions. The
deterministic build flavor reduces host drift; it is not a compatibility layer
between Rapier versions.

## Required Match Set

Cross-host parity requires all of the following to match:

- Rapier core version
- deterministic build flavor
- solver and integration settings that affect the step
- timestep and gravity
- stable object creation order
- tick-level input event order
- snapshot compatibility policy
- canonical hash schema

Creation, mutation, and destruction inputs must be ordered by a total key such
as `(tick, sequence, authorId, eventId)`. A parity fixture's `bodies` array is
the stable initial-world creation order.

## Stable IDs

Rapier internal handles are not stable across hosts or across different
creation/deletion histories. The canonical hash therefore uses a 64-bit stable
id derived from the Scene Sync object id:

```text
stableId = FNV-1a-64(UTF-8(objectId))
```

Unity exposes `RapierWorld.StableIdHash(string)` plus
`SetRigidBodyStableId(...)` and `SetColliderStableId(...)` so Scene Sync can
attach this id after creating each native body/collider. If no stable id is set,
the Unity FFI falls back to Rapier handles for same-host replay diagnostics only.

## Canonical Hash V1

`SceneSyncCanonicalPhysicsHashV1` uses FNV-1a-64 over little-endian fields:

- hash name
- engine name: `rapier`
- Rapier core version
- gravity
- timestep
- rigid bodies sorted by stable id
- colliders sorted by stable id

Rigid body fields:

- stable id
- body type
- linear damping
- angular damping
- additional solver iterations
- CCD enabled
- can sleep
- translation
- rotation quaternion
- linear velocity
- angular velocity
- sleeping
- enabled

Collider fields:

- stable id
- parent body stable id
- local translation and rotation
- shape type and shape parameters
- density
- friction and combine rule
- restitution and combine rule
- sensor
- enabled

The canonical hash is an exact raw-`f32` hash. Quantized parity hashes can be
added as a separate mode, but exact hashing is the default divergence detector.

## Snapshot Policy

There are two snapshot layers:

- Native Rapier snapshot: fast rollback/resync for the same Rapier core and FFI
  snapshot format.
- Scene Sync canonical physics snapshot: versioned cross-host schema for
  body/collider/settings state.

The current Unity FFI implements the native Rapier snapshot layer. It includes a
format name, format version, FFI schema version, and Rapier core version and
rejects incompatible bytes. It is not the long-term cross-host canonical
snapshot schema.

## Browser Notes

The browser runtime must import the deterministic compat package:

```js
import RAPIER from '@dimforge/rapier3d-deterministic-compat';
```

Exported viewers should map that specifier to the bundled deterministic
`rapier.mjs` asset.

## Cross-Host Fixture v0

The shared fixtures live under:

```text
fixtures/rapier/
```

They use `SceneSyncRapierParity-0.30`, Rapier core `0.30.0`, and sample ticks
through tick 600.

The staged fixture set provides narrower parity coverage for regression
isolation:

- `parity-freefall-001.json`: no contact through tick 600, used to confirm free
  rigid-body integration.
- `parity-contact-basic-001.json`: one fixed floor and one vertically falling
  dynamic box with zero friction, zero restitution, zero angular velocity, no
  damping, `canSleep: false`, and explicit combine rules.
- `parity-basic-001.json`: the original floor + moving/rotating box case. It
  has been manually validated in Unity 6000 and matches the Browser hashes
  through tick 600. Keep the staged fixtures around to isolate future
  contact/material/solver regressions from initial-state setup issues.

Browser support lives in `afjk.jp` as `rapier-parity-fixture.js` and records
`world.canonicalStateHash()` plus `canonicalStateDump()` for mismatch debugging.

Unity support lives in the `CrossHostParity` sample. It loads the selected
fixture, creates bodies in fixture order, assigns body/collider stable ids with
`RapierWorld.StableIdHash(objectId)`, and logs a JSON result using the same
`hashes` / `dumps` shape. Unity validation is manual until an Editor CI job
exists.
