# Native Build

The native plugin is implemented in Rust and exposes a small C ABI over `rapier3d`.

## Build

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

The output is written to `native/target/release/`:

- macOS: `librapier_unity_ffi.dylib`
- Windows: `rapier_unity_ffi.dll`
- Linux: `librapier_unity_ffi.so`
- Static library: platform-specific `librapier_unity_ffi.a` where supported

Copy the platform binary into the Unity package plugin folder expected by your project, for example:

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
Packages/com.afjk.rapier/Runtime/Plugins/Windows/rapier_unity_ffi.dll
```

## Platform Notes

The C# binding uses this library name:

```csharp
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
private const string DllName = "__Internal";
#else
private const string DllName = "rapier_unity_ffi";
#endif
```

Initial development targets macOS Editor and Windows Editor. Mobile, XR, and WebGL builds need platform-specific native build, packaging, and CI work.

## Determinism

The Rust crate targets the Scene Sync parity profile by pinning `rapier3d = "=0.30.0"` and enabling Rapier's `enhanced-determinism` feature. Deterministic behavior still depends on platform, compiler flags, stable setup ordering, and avoiding nondeterministic gameplay code around the physics step.

The current state hash implements `SceneSyncCanonicalPhysicsHashV1`. Set stable ids on bodies and colliders before comparing cross-host hashes; without stable ids the hash falls back to Rapier handles and is only useful for same-host replay diagnostics.

Snapshot APIs serialize a native Rapier snapshot with a format/version header and restore it only when the native format and Rapier core version match. This is the fast rollback/resync layer, not the long-term Scene Sync canonical snapshot schema.
