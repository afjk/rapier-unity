# Deterministic Replay

Open `DeterministicReplay.unity` and enter Play Mode.

The scene creates two explicit `RapierWorld` instances through the low-level API. Both worlds receive the same setup:

- fixed floor
- dynamic box
- gravity
- timestep
- creation order

The sample steps both worlds for 600 ticks and compares `StateHash()` after every tick. It logs an error if the hashes diverge and shows the final hash values in the Game view.

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

The current hash is intended for same-version comparisons. Cross-platform and cross-version guarantees require more validation and a versioned snapshot format.
