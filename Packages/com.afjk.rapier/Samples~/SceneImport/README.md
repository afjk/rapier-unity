# Scene Import (Scene Sync foundation)

Demonstrates path-2 of the project goals — **generating a Rapier scene from an
importer** — using the neutral `RapierSceneImporter`.

Open `SceneImport.unity` and enter Play Mode. A fixed floor plus a few falling
primitives are created entirely from a data description, not hand-placed.

## How it works

1. A neutral `RapierSceneDescription` (world gravity/timestep + a list of bodies,
   each with colliders) describes the scene. It contains **no** Scene Sync or
   network concepts.
2. `RapierSceneImporter.Import(description, parent)` creates a
   `RapierWorldComponent` and, for each body, a GameObject with a
   `RapierRigidBodyComponent`, its collider components, and a
   `RapierImportedObject` metadata component (which records the source system /
   id / order).
3. The importer sets `StableId` and `RegistrationOrder` from the description and
   calls `RapierWorldComponent.RebuildWorld()`, so the world is built in one
   deterministic pass — the same description yields the same world on every host.

The sample either imports the assigned `scene-import-example.json` (the default)
or, if none is assigned, a small built-in description. Toggle **Debug draw
colliders** to overlay the native collider geometry.

## Where Scene Sync fits

This sample and the importer are deliberately **Scene Sync-agnostic**. A real
Scene Sync importer lives *downstream* of this package: it converts Scene Sync's
own scene/physics data (object ids, registration order, physics metadata, synced
GLB assets) into a `RapierSceneDescription` and calls `RapierSceneImporter`. The
core Rapier components never depend on Scene Sync; only the downstream adapter and
the `RapierImportedObject` metadata carry source-specific information.

## JSON format

See `scene-import-example.json`. Enum fields are integers:

- `bodyType`: `0` Dynamic, `1` Fixed, `2` KinematicPositionBased, `3` KinematicVelocityBased
- collider `shape`: `0` Box, `1` Sphere, `2` Capsule
- `registrationMode`: `0` HierarchyOrder, `1` StableId, `2` ExplicitOrder

## Native Plugin

Build and copy the native plugin before running (see the package README). If it is
missing, the sample shows a warning instead of throwing.
