# Rapier for Unity

This Unity package exposes Rapier through an explicit-world API. It does not replace Unity Physics and does not scan Unity `Rigidbody` or built-in collider components automatically.

## Install Locally

Add the package from disk in Unity Package Manager:

```text
Packages/com.afjk.rapier
```

or add a local package entry to your Unity project's `Packages/manifest.json`.

## Native Library

Build the native plugin before entering play mode:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

Then copy the resulting platform library into your project's plugin folder, such as:

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
```

## API Shape

Use `RapierWorld` for low-level code and `RapierWorldComponent` for scene-authored worlds. Both are explicit world owners, so multiple worlds are supported.

## Samples

Unity Package Manager exposes two samples:

- `Basic Falling Ball`: a component API scene with a fixed floor and dynamic sphere.
- `Deterministic Replay`: a low-level API scene that compares two identical worlds by state hash.

Import a sample from Package Manager, open its scene, and enter Play Mode after the native plugin is built and copied into `Runtime/Plugins`.
