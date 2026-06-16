# Cross-Host Parity

Runs the shared Scene Sync Rapier parity fixture and logs a JSON result that can
be compared with the Browser fixture runner.

Fixture:

```text
fixtures/rapier/parity-basic-001.json
```

The runner creates one explicit `RapierWorld`, creates bodies in stable object
id order, assigns stable ids with `RapierWorld.StableIdHash(objectId)`, steps
to the fixture sample ticks, and records `StateHash()` as 16-character hex.

## Manual Check

1. Build and copy the native plugin:

   ```sh
   cd native
   cargo build --release -p rapier_unity_ffi
   ```

2. Open the Unity 6000 dev project or import this sample into a Unity project.
3. Create an empty GameObject and add `CrossHostParityRunner`.
4. Enter Play Mode.
5. Copy the logged JSON and compare `hashes` with the Browser result.

If the hashes differ, compare the same tick under `dumps`. The dump includes
the canonical body and collider fields used to diagnose initial pose, velocity,
damping, material, and timestep mismatches.
