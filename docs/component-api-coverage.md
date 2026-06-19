# Component ↔ Low-level API Mapping

This document maps every public `RapierWorld` (low-level) API to its Unity
**component-layer** equivalent. It is the source of truth for "what can I do from
components vs. what still needs the low-level handle API".

It complements [api-coverage.md](api-coverage.md), which tracks coverage against
the Rapier JS / Rapier core feature set. This file instead tracks the **internal**
low-level → component mapping.

## Legend

| Symbol | Meaning |
|---|---|
| ✅ | Wrapped by a component / component-friendly façade |
| ◐ | Partially wrapped (e.g. authoring + setter, but no live getter) |
| ➖ | Not wrapped; use the low-level API via `RapierWorldComponent.World` |

Every component can still reach the low-level world through
`RapierWorldComponent.World` (or `RapierRigidBodyComponent.World`), so ➖ rows are
always available — they just don't have a dedicated component member yet.

---

## World

| Low-level (`RapierWorld`) | Component | Status |
|---|---|---|
| `Create()` / `Dispose()` | `RapierWorldComponent` lifecycle (`EnsureWorld`, `OnDisable`) | ✅ |
| `SetGravity` | `RapierWorldComponent.Gravity` | ✅ |
| `SetTimestep` | `RapierWorldComponent.Timestep` | ✅ |
| `Step` | `RapierWorldComponent.Step()` / `StepMode.FixedUpdate` | ✅ |
| `StateHash` | `RapierWorldComponent.StateHash()` | ✅ |
| `SnapshotSize` | `RapierWorldComponent.SnapshotSize()` | ✅ |
| `TryCreateSnapshot` / `TryReadSnapshot` | `RapierWorldComponent.TryCreateSnapshot` / `TryReadSnapshot` | ✅ |
| `TryWriteSnapshot` | (use `TryCreateSnapshot`) | ➖ |
| `DrainCollisionEvents` / `DrainContactForceEvents` | `RapierWorldComponent.DrainCollisionEvents` / `DrainContactForceEvents` | ✅ |
| `DebugRender` | `RapierDebugRenderer.DrawRuntimeLines`, or `RapierWorldComponent.World.DebugRender` | ✅ |
| `InteractionGroups` (static) | `RapierWorld.InteractionGroups` (shared static) | ✅ |
| `StableIdHash` (static) | `RapierStableId` / set via `StableId` + `AutoGenerateStableId` | ✅ |
| — | `RapierWorldComponent.RebuildWorld()` + `RegistrationMode` (deterministic order) | ✅ component-only |

---

## Rigid body

| Low-level (`RapierWorld`) | Component (`RapierRigidBodyComponent`) | Status |
|---|---|---|
| `CreateRigidBody` | the component itself (`BodyType`, velocities, damping, …) | ✅ |
| `DestroyRigidBody` | `Unregister()` / disable | ✅ |
| `SetRigidBodyStableId` | `StableId` + `AutoGenerateStableId` | ✅ |
| `TryGetTransform` | `TryGetTransform` | ✅ |
| `SetTransform` | `PushTransformToRapier()` / transform sync | ✅ |
| `TryGetRigidBodyState` | — | ➖ |
| `TryGetLinearVelocity` / `SetLinearVelocity` | `TryGetLinearVelocity` / `SetLinearVelocity` | ✅ |
| `TryGetAngularVelocity` / `SetAngularVelocity` | `TryGetAngularVelocity` / `SetAngularVelocity` | ✅ |
| `SetLinearDamping` / `SetAngularDamping` | `SetLinearDamping` / `SetAngularDamping` + `LinearDamping`/`AngularDamping` authoring | ◐ (no live getter) |
| `SetGravityScale` | `SetGravityScale` + `GravityScale` authoring | ◐ |
| `SetCcdEnabled` | `SetCcdEnabled` + `CcdEnabled` authoring | ◐ |
| `SetSoftCcdPrediction` | `SetSoftCcdPrediction` + `SoftCcdPrediction` authoring | ◐ |
| `SetBodyEnabled` | `SetBodyEnabled` | ✅ |
| `AddForce` / `AddTorque` | `AddForce` / `AddTorque` | ✅ |
| `ApplyImpulse` / `ApplyTorqueImpulse` | `ApplyImpulse` / `ApplyTorqueImpulse` | ✅ |
| `AddForceAtPoint` / `ApplyImpulseAtPoint` | `AddForceAtPoint` / `ApplyImpulseAtPoint` | ✅ |
| `SetNextKinematicTranslation` / `SetNextKinematicRotation` | same names | ✅ |
| `SetEnabledRotations` / `SetEnabledTranslations` | same names + `SetLockedRotations`/`SetLockedTranslations` authoring | ✅ |
| `SetBodySleeping` | `SetSleeping` | ✅ |
| `SetAdditionalSolverIterations` | `SetAdditionalSolverIterations` + `AdditionalSolverIterations` authoring | ◐ |
| `TryGetMass` | `TryGetMass` | ✅ |
| `SetDominanceGroup` | `SetDominanceGroup` + `DominanceGroup` authoring | ◐ |
| (other live `TryGet*`: damping, gravity scale, CCD, enabled, solver iterations, dominance) | — | ➖ |
| — | `RegistrationOrder` (deterministic order) | ✅ component-only |

---

## Collider

| Low-level (`RapierWorld`) | Component | Status |
|---|---|---|
| `CreateBoxCollider` | `RapierBoxCollider` | ✅ |
| `CreateSphereCollider` | `RapierSphereCollider` | ✅ |
| `CreateCapsuleCollider` | `RapierCapsuleCollider` | ✅ |
| `CreateTrimeshCollider` | `RapierTrimeshCollider` (or `RapierMeshCollider`, non-convex) | ✅ |
| `CreateConvexHullCollider` | `RapierConvexHullCollider` (or `RapierMeshCollider`, convex) | ✅ |
| `CreateHeightfieldCollider` | `RapierHeightfieldCollider` | ✅ |
| `CreateVoxelsCollider` | `RapierVoxelsCollider` | ✅ |
| `DestroyCollider` | `Unregister()` / disable | ✅ |
| `SetColliderStableId` | `StableId` + `AutoGenerateStableId` | ✅ |
| `SetColliderFriction` | `SetFriction` + `Friction` authoring | ◐ |
| `SetColliderRestitution` | `SetRestitution` + `Restitution` authoring | ◐ |
| `SetColliderDensity` | `SetDensity` + `Density` authoring | ◐ |
| `SetColliderSensor` | `SetSensor` + `IsSensor` authoring | ◐ |
| `SetColliderEnabled` | `SetColliderEnabled` | ✅ |
| `SetColliderFrictionCombineRule` / `SetColliderRestitutionCombineRule` | `SetFrictionCombineRule` / `SetRestitutionCombineRule` + authoring | ✅ |
| `SetColliderCollisionGroups` | `SetCollisionGroups` / `SetAuthoredCollisionGroups` | ✅ |
| `SetColliderSolverGroups` | `SetSolverGroups` + authoring | ✅ |
| `SetColliderActiveEvents` | `SetActiveEvents` + authoring | ✅ |
| `SetColliderActiveCollisionTypes` | `SetActiveCollisionTypes` + authoring | ✅ |
| `SetColliderContactForceEventThreshold` | `SetContactForceEventThreshold` + authoring | ✅ |
| `SetColliderTranslationWrtParent` / `SetColliderPositionWrtParent` | `LocalPosition` / `LocalRotation` authoring (applied on create) | ◐ (no live setter) |
| (collider live `TryGet*`) | — | ➖ |
| — | `RegistrationOrder` (deterministic order) | ✅ component-only |

---

## Scene queries

All wrapped by the `RapierPhysics` static façade (accepts a `RapierWorld` or a
`RapierWorldComponent`).

| Low-level (`RapierWorld`) | Component (`RapierPhysics`) | Status |
|---|---|---|
| `Raycast` | `RapierPhysics.Raycast` | ✅ |
| `RaycastFiltered` | `RapierPhysics.RaycastFiltered` | ✅ |
| `RaycastAll` | `RapierPhysics.RaycastAll` | ✅ |
| `TryProjectPoint` | `RapierPhysics.ProjectPoint` | ✅ |
| `TryIntersectionWithPoint` | `RapierPhysics.IntersectionWithPoint` | ✅ |
| `CastShape` | `RapierPhysics.CastShape` | ✅ |
| `IntersectShape` | `RapierPhysics.IntersectShape` | ✅ |

---

## Joints

| Low-level (`RapierWorld`) | Component | Status |
|---|---|---|
| `CreateFixedJoint` | `RapierFixedJointComponent` | ✅ |
| `CreateSphericalJoint` | `RapierSphericalJointComponent` | ✅ |
| `CreateRevoluteJoint` | `RapierRevoluteJointComponent` | ✅ |
| `CreatePrismaticJoint` | `RapierPrismaticJointComponent` | ✅ |
| `CreateRopeJoint` | `RapierRopeJointComponent` | ✅ |
| `CreateSpringJoint` | `RapierSpringJointComponent` | ✅ |
| `RemoveJoint` | `RapierJointComponent.RemoveJoint()` / disable | ✅ |
| `SetJointLimits` | `RapierJointComponent.SetLimits` (cached, reapplied on rebuild) | ✅ |
| `SetJointMotorPosition` / `SetJointMotorVelocity` / `SetJointMotorMaxForce` | `SetMotorPosition` / `SetMotorVelocity` / `SetMotorMaxForce` (cached) | ✅ |
| — | `StableId` + `AutoGenerateStableId`, `RegistrationOrder` | ✅ component-only |

---

## Character controller

| Low-level (`RapierWorld`) | Component (`RapierCharacterControllerComponent`) | Status |
|---|---|---|
| `MoveCharacter` | `Move` / `ComputeMovement` (+ `Controller`, `Shape`, `QueryFilter`) | ✅ |

---

## PID controller

| Low-level (`RapierWorld`) | Component (`RapierPidControllerComponent`) | Status |
|---|---|---|
| `CreatePidController` | the component itself (`Kp`/`Ki`/`Kd`/`Axes`, lazy `EnsureController`) | ✅ |
| `DestroyPidController` | `OnDisable` | ✅ |
| `SetPidControllerAxes` | `Axes` | ✅ |
| `ResetPidControllerIntegrals` | `ResetIntegrals()` | ✅ |
| `ApplyPidLinearCorrection` | `ApplyLinearCorrection` | ✅ |
| `ApplyPidAngularCorrection` | `ApplyAngularCorrection` | ✅ |

---

## Known component-layer gaps (use the low-level world)

These have no dedicated component member yet; reach them through
`RapierWorldComponent.World` / `RapierRigidBodyComponent.World`:

- Live getters for body damping, gravity scale, CCD, soft-CCD, enabled, solver
  iterations, dominance group (`TryGet*`). Components expose the setters and
  serialized authoring values, not live reads.
- `TryGetRigidBodyState` (combined state read).
- Live collider getters (`TryGetCollider*`) and live `SetColliderTranslation/PositionWrtParent`.
- `TryWriteSnapshot` to a caller-provided buffer (use `TryCreateSnapshot`).
