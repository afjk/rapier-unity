# Snapshot Design Notes

Snapshot APIs are present in the C ABI but currently stubbed:

```c
size_t rapier_unity_world_snapshot_size(uint64_t world);
bool rapier_unity_world_snapshot_write(uint64_t world, uint8_t* out_bytes, size_t len);
bool rapier_unity_world_snapshot_read(uint64_t world, const uint8_t* bytes, size_t len);
```

The intended design is a versioned binary format that can restore a world for replay and rollback.

## Requirements

- Include a format magic and version.
- Include the Rapier crate version and FFI schema version.
- Serialize deterministic world settings such as gravity and timestep.
- Serialize rigid bodies, colliders, joints, and eventually event configuration.
- Preserve stable handle mapping or provide a remap table.
- Reject snapshots with incompatible versions or malformed lengths.
- Keep byte order and float representation explicit.
- Add tests for roundtrip restore and hash equality.

## Current Stub Behavior

- `snapshot_size` returns `0`.
- `snapshot_write` returns `true` only for a zero-length write.
- `snapshot_read` returns `false`.

This keeps the public ABI shape stable while making it impossible to accidentally treat the current stubs as usable snapshot data.

