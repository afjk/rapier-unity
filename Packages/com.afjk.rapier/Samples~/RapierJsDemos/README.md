# Rapier JS Demos

Ports the Rapier 3D JavaScript demo catalog into a Unity runtime sample.

Source demo:

```text
https://rapier.rs/demos3d/index.html
```

The sample generates its world at runtime with the low-level `RapierWorld` API
and visual-only Unity primitives. Add `RapierJsDemosSample` to an empty
GameObject, enter Play Mode, and switch demos from the on-screen menu.

## Ported Demos

These demos run with the Unity API exposed today:

- `pyramid`
- `keva tower`
- `damping`
- `CCD`
- `fountain`

`fountain` is adapted to cycle through boxes, spheres, and capsules because
cone/cylinder colliders are not exposed yet.

## Cataloged But Not Ported Yet

These entries remain in the menu and show an unsupported message:

- `collision groups`: needs collider collision-group APIs
- `character controller`: needs the Rapier character controller API
- `convex polyhedron`: needs convex hull collider APIs
- `heightfield`: needs heightfield collider APIs
- `joints`: needs impulse/multibody joint APIs
- `locked rotations`: needs axis locking APIs
- `platform`: needs runtime kinematic velocity or next-position APIs
- `triangle mesh`: needs triangle mesh collider APIs

## Native Plugin

Build and copy the native plugin before running the sample:

```sh
cd native
cargo build --release -p rapier_unity_ffi
```

Then copy the platform binary into the package plugin folder, for example:

```text
Packages/com.afjk.rapier/Runtime/Plugins/macOS/librapier_unity_ffi.dylib
```

If the native plugin is missing, the sample shows a warning instead of throwing
during setup.
