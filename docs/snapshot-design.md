# Snapshot Design Notes

Snapshot APIs are present in the C ABI:

```c
size_t rapier_unity_world_snapshot_size(uint64_t world);
bool rapier_unity_world_snapshot_write(uint64_t world, uint8_t* out_bytes, size_t len);
bool rapier_unity_world_snapshot_read(uint64_t world, const uint8_t* bytes, size_t len);
```

The implemented native format is a versioned binary snapshot that can restore a
world for same-profile replay and rollback.

## Requirements

- Include a format magic and version.
- Include the Rapier crate version and FFI schema version.
- Serialize deterministic world settings such as gravity and timestep.
- Serialize rigid bodies, colliders, joints, and eventually event configuration.
- Preserve stable handle mapping or provide a remap table.
- Reject snapshots with incompatible versions or malformed lengths.
- Keep byte order and float representation explicit.
- Add tests for roundtrip restore and hash equality.

## Current Native Behavior

- `snapshot_size` returns the serialized native snapshot size.
- `snapshot_write` requires an exact-size output buffer.
- `snapshot_read` rejects malformed bytes or incompatible format/core versions.
- Roundtrip restore preserves the canonical state hash and future deterministic
  steps for the same Rapier core/profile.

This native snapshot is intended for fast rollback/resync inside a matching
Unity FFI build. It is not the cross-host Scene Sync canonical physics snapshot.
The canonical snapshot should remain a separate versioned schema over
body/collider/settings state.
