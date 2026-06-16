# Scene Sync Parity Profile

This profile defines the minimum contract for comparing Rapier physics state
across Scene Sync hosts such as Unity, Browser, and Godot.

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
as `(tick, sequence, authorId, eventId)`. When building an initial world, create
bodies and colliders in stable Scene Sync object id order.

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
- activation thresholds and sleep timer
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
