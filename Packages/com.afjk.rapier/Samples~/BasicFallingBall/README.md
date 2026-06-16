# Basic Falling Ball

This sample plan creates one explicit Rapier world, a fixed floor, and a dynamic sphere.

## Scene Setup

1. Create an empty GameObject named `Rapier World`.
2. Add `RapierWorldComponent`.
3. Enable `Log State Hash` if you want a per-step hash in the console.
4. Create a floor GameObject at `(0, -0.5, 0)`.
5. Add `RapierRigidBodyComponent`, set `Body Type` to `Fixed`, and add `RapierBoxCollider` with large half extents.
6. Create a ball GameObject at `(0, 5, 0)`.
7. Add `RapierRigidBodyComponent`, set `Body Type` to `Dynamic`, and add `RapierSphereCollider`.
8. Parent the floor and ball under `Rapier World`, or assign their `World Component` field explicitly.

The world steps in `FixedUpdate` by default and each body can sync its transform from Rapier.

## Low-level Equivalent

```csharp
using AFJK.Rapier;
using UnityEngine;

using var world = RapierWorld.Create();
world.SetGravity(new Vector3(0, -9.81f, 0));
world.SetTimestep(1f / 60f);

var floor = world.CreateRigidBody(RapierBodyDesc.Fixed(new Vector3(0, -0.5f, 0)));
world.CreateBoxCollider(floor, new RapierBoxColliderDesc
{
    HalfExtents = new Vector3(10, 0.5f, 10),
    Density = 0f,
    LocalRotation = Quaternion.identity
});

var ball = world.CreateRigidBody(RapierBodyDesc.Dynamic(new Vector3(0, 5, 0)));
world.CreateSphereCollider(ball, RapierSphereColliderDesc.Unit);

world.Step();
Debug.Log(world.StateHash());
```

