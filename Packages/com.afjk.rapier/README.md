# Rapier for Unity

This Unity package exposes Rapier through an explicit-world API. It does not replace Unity Physics and does not scan Unity `Rigidbody` or built-in collider components automatically.

## Install Locally

Add the package from disk in Unity Package Manager:

```text
Packages/com.afjk.rapier
```

or add a local package entry to your Unity project's `Packages/manifest.json`.

## Native Library

Prebuilt native FFI plugins are bundled in `Runtime/Plugins` and load
automatically in the editor and standalone builds:

- Windows: `Windows/rapier_unity_ffi.dll` (x86_64)
- Linux: `Linux/librapier_unity_ffi.so` (x86_64)
- macOS: `macOS/librapier_unity_ffi.dylib` (Apple Silicon / arm64)

To rebuild from source (for example to target an Intel mac or refresh the
binary), build the crate and copy the platform library into the matching
plugin folder:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
```

## API Shape

Use `RapierWorld` for low-level code and `RapierWorldComponent` for scene-authored worlds. Both are explicit world owners, so multiple worlds are supported.

## Samples

Unity Package Manager exposes three samples:

- `Basic Falling Ball`: a component API scene with a fixed floor and dynamic sphere.
- `Deterministic Replay`: a low-level API scene that compares two identical worlds by state hash.
- `Cross-Host Parity`: a low-level runner that loads `fixtures/rapier/parity-basic-001.json` and logs Browser-comparable canonical hash JSON.

Import a sample from Package Manager, open its scene, and enter Play Mode after the native plugin is built and copied into `Runtime/Plugins`.
