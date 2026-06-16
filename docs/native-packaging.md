# Native Packaging Notes

The first native build path is intentionally manual:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

Copy the resulting platform library into a Unity plugin folder that matches the target platform:

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
Packages/com.afjk.rapier/Runtime/Plugins/Windows/rapier_unity_ffi.dll
Packages/com.afjk.rapier/Runtime/Plugins/Linux/librapier_unity_ffi.so
```

Future packaging work should add:

- A repeatable copy/sign/notarize path for macOS Editor builds.
- Windows and Linux Editor plugin import metadata.
- Static library support for iOS.
- Android ABI builds.
- XR platform validation.
- CI artifacts for native plugin builds.

