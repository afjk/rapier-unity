# Basic Falling Ball

Open `BasicFallingBall.unity` and enter Play Mode.

The scene contains a small bootstrap script that creates:

- one explicit `RapierWorldComponent`
- one fixed Rapier floor body
- one dynamic Rapier sphere body
- Rapier colliders for both bodies
- visual-only Unity primitives with Unity colliders removed

The sample uses `RapierWorldComponent` stepping in `FixedUpdate`, syncs the dynamic body's transform back to Unity, and logs a state hash once per simulated second.

## Native Plugin

Build and copy the native plugin before running the scene:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

Then copy the platform binary into the package plugin folder, for example:

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
```

If the native plugin is missing, the scene shows a warning instead of throwing during setup.
