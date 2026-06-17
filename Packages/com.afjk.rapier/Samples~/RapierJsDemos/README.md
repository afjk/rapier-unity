# Rapier JS Demos

Ports the Rapier 3D JavaScript demo catalog into a Unity runtime sample.

Source demo:

```text
https://rapier.rs/demos3d/index.html
```

Open `RapierJsDemos.unity` and enter Play Mode.

The sample generates its world at runtime with the low-level `RapierWorld` API
and visual-only Unity primitives. Switch demos from the on-screen menu.

## Ported Demos

All entries in the on-screen menu are ported and run with the Unity API:

- `pyramid`
- `keva tower`
- `damping`
- `CCD`
- `fountain` (cycles boxes/spheres/capsules; cone/cylinder colliders are not exposed yet)
- `collision groups` (per-collider interaction groups)
- `joints` (a spherical-joint chain)
- `PID controller` (dynamic cylinder body driven by Rapier PID correction)
- `platform` (kinematic-position platform via set-next-kinematic-translation)
- `locked rotations` (per-axis rotation locks)
- `convex polyhedron` (convex hull colliders)
- `triangle mesh` (trimesh ground + generated visual mesh)
- `heightfield` (heightfield ground + generated visual mesh)
- `voxels` (voxel collider terrain + generated voxel visual mesh)
- `character controller` (kinematic capsule moved with the character controller, autostep + snap-to-ground)

The GLB asset demos are not included yet because this sample currently uses
runtime-generated Unity geometry only.

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
