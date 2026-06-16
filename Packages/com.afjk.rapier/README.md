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

