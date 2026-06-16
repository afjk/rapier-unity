# Cross-Host Parity

Runs the shared Scene Sync Rapier parity fixtures and logs a JSON result that can
be compared with the Browser fixture runner.

Fixtures:

```text
fixtures/rapier/parity-basic-001.json
fixtures/rapier/parity-freefall-001.json
fixtures/rapier/parity-contact-basic-001.json
```

The runner creates one explicit `RapierWorld`, creates bodies in fixture array
order, assigns stable ids with `RapierWorld.StableIdHash(objectId)`, steps to
the fixture sample ticks, and records `StateHash()` as 16-character hex.

## Manual Check

1. Build and copy the native plugin:

   ```sh
   cd native
   cargo build --release -p rapier_unity_ffi
   ```

2. Open the Unity 6000 dev project or import this sample into a Unity project.
   The sample includes its own fixture copies under `fixtures/rapier/`.
3. Create an empty GameObject and add `CrossHostParityRunner`.
4. Leave `fixtureJson` empty to run `parity-basic-001.json`, or assign one of
   the fixture TextAssets to run `parity-freefall-001.json` or
   `parity-contact-basic-001.json`.
5. Enter Play Mode.
6. Copy the logged JSON and compare `hashes` with the Browser result for the
   same fixture.

If the hashes differ, compare the same tick under `dumps`. The dump includes
the canonical body and collider fields used to diagnose initial pose, velocity,
damping, material, and timestep mismatches.
