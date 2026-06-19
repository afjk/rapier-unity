# API Coverage Matrix

This document tracks Rapier JS 0.19.3 / Rapier core 0.30.0 API coverage in Rapier for Unity.

## Baseline and goal

The short-term API baseline is **rapier.js 0.19.3** because this is the browser parity target (see [scenesync-parity.md](scenesync-parity.md)). The goal is to make official Rapier JS demos portable to Unity where practical.

Coverage is tracked separately for:

- **Low-level C# API** — `RapierWorld` and related types
- **Unity Component API** — `RapierWorldComponent`, `RapierRigidBodyComponent`, `RapierColliderComponent`
- **Native FFI** — Rust/C ABI exposed by `rapier_unity_ffi`
- **Samples/tests** — Unity samples and parity fixtures

## Status labels

| Label | Meaning |
|---|---|
| Supported | Working and validated |
| Partial | Some coverage, gaps remain |
| Missing | Not yet implemented |
| Planned | Committed future work |
| N/A | Not applicable to this layer |

## Priority labels

| Priority | Meaning |
|---|---|
| S | Needed for JS demo parity and Scene Sync |
| A | Important general Rapier feature |
| B | Useful but not blocking |
| C | Later |

---

## 1. World

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| create / destroy world | Supported | Supported | Supported | S |
| set gravity | Supported | Supported | Supported | S |
| set timestep | Supported | Supported | Supported | S |
| step | Supported | Supported | Supported | S |
| take snapshot | Supported | Supported | Supported | A |
| restore snapshot | Supported | Supported | Supported | A |
| state hash (native) | Supported | Supported | Supported | S |
| canonical hash | Supported | Supported | Supported | S |
| debug render | Supported | Partial | Supported | B |
| event queue | Supported | Supported | Supported | A |
| deterministic registration order / rebuild | N/A | Supported | N/A | S |
| integration parameters / solver settings | Partial | Missing | Partial | B |

---

## 2. RigidBody

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| create fixed body | Supported | Supported | Supported | S |
| create dynamic body | Supported | Supported | Supported | S |
| create kinematic position-based body | Supported | Supported | Supported | A |
| create kinematic velocity-based body | Supported | Supported | Supported | A |
| destroy body | Supported | Supported | Supported | S |
| get / set translation | Supported | Supported | Supported | S |
| get / set rotation | Supported | Supported | Supported | S |
| get / set linvel | Supported | Supported | Supported | S |
| get / set angvel | Supported | Supported | Supported | S |
| get / set gravity scale | Supported | Supported | Supported | A |
| get / set linear damping | Supported | Supported | Supported | A |
| get / set angular damping | Supported | Supported | Supported | A |
| get / set additional solver iterations | Supported | Supported | Supported | B |
| get / set CCD enabled | Supported | Supported | Supported | A |
| get / set enabled | Supported | Supported | Supported | A |
| sleep / wake | Supported | Supported | Supported | B |
| add force | Supported | Supported | Supported | A |
| add torque | Supported | Supported | Supported | A |
| apply impulse | Supported | Supported | Supported | A |
| apply torque impulse | Supported | Supported | Supported | A |
| add force at point | Supported | Supported | Supported | B |
| apply impulse at point | Supported | Supported | Supported | B |
| set next kinematic translation | Supported | Supported | Supported | A |
| set next kinematic rotation | Supported | Supported | Supported | A |
| lock/enable rotations (per axis) | Supported | Supported | Supported | A |
| lock/enable translations (per axis) | Supported | Supported | Supported | A |
| mass / mass properties getters | Supported | Supported | Supported | B |
| dominance group | Supported | Supported | Supported | C |
| user data / stable id | Supported | Supported | Supported | S |

---

## 3. Collider

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| cuboid | Supported | Supported | Supported | S |
| ball | Supported | Supported | Supported | S |
| capsule | Supported | Supported | Supported | S |
| trimesh | Supported | Supported | Supported | A |
| convex hull | Supported | Supported | Supported | A |
| heightfield | Supported | Supported | Supported | B |
| voxels | Supported | Supported | Supported | B |
| round shapes | Missing | Missing | Missing | C |
| sensor | Supported | Supported | Supported | A |
| enabled | Supported | Supported | Supported | A |
| density | Supported | Supported | Supported | S |
| mass (override) | Missing | Missing | Missing | B |
| friction | Supported | Supported | Supported | S |
| restitution | Supported | Supported | Supported | S |
| friction combine rule | Supported | Supported | Supported | A |
| restitution combine rule | Supported | Supported | Supported | A |
| collision groups | Supported | Supported | Supported | A |
| solver groups | Supported | Supported | Supported | A |
| active events | Supported | Supported | Supported | A |
| active collision types | Supported | Supported | Supported | B |
| active hooks | Missing | Missing | Missing | C |
| parent body | Supported | Supported | Supported | S |
| local translation | Supported | Supported | Supported | A |
| local rotation | Supported | Supported | Supported | A |
| stable id | Supported | Supported | Supported | S |

---

## 4. Scene Queries

Component-API coverage is provided by the `RapierPhysics` static façade, which
accepts either a `RapierWorld` or a `RapierWorldComponent`.

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| castRay | Supported | Supported | Supported | S |
| castRayAndGetNormal | Supported | Supported | Supported | A |
| intersectionsWithRay / raycast all | Supported | Supported | Supported | A |
| projectPoint | Supported | Supported | Supported | A |
| intersectionsWithPoint | Supported | Supported | Supported | B |
| castShape | Supported | Supported | Supported | A |
| intersectionsWithShape | Supported | Supported | Supported | B |
| query filters | Supported | Supported | Supported | A |
| exclude collider / body filter | Supported | Supported | Supported | A |
| collision groups filter | Supported | Supported | Supported | A |

---

## 5. Events

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| collision started | Supported | Supported | Supported | A |
| collision stopped | Supported | Supported | Supported | A |
| intersection started | Supported | Supported | Supported | A |
| intersection stopped | Supported | Supported | Supported | A |
| contact force events | Supported | Supported | Supported | B |
| contact pairs | Missing | Missing | Missing | B |
| contact manifolds | Missing | Missing | Missing | C |
| event queue drain API | Supported | Supported | Supported | A |

---

## 6. Joints

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| fixed joint | Supported | Supported | Supported | A |
| spherical joint | Supported | Supported | Supported | A |
| revolute joint | Supported | Supported | Supported | A |
| prismatic joint | Supported | Supported | Supported | A |
| rope joint | Supported | Supported | Supported | B |
| spring joint | Supported | Supported | Supported | B |
| generic joint | Missing | Missing | Missing | C |
| joint motors | Supported | Supported | Supported | B |
| joint limits | Supported | Supported | Supported | A |
| remove joint | Supported | Supported | Supported | A |

---

## 7. Character Controller

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| create controller | Supported | Supported | Supported | A |
| compute collider movement | Supported | Supported | Supported | A |
| computed movement result | Supported | Supported | Supported | A |
| collisions output | Missing | Missing | Missing | A |
| autostep | Supported | Supported | Supported | B |
| snap to ground | Supported | Supported | Supported | B |
| slope settings | Supported | Supported | Supported | B |
| up vector | Supported | Supported | Supported | A |
| apply impulses to dynamic bodies | Missing | Missing | Missing | B |
| query filters | Supported | Supported | Supported | A |
| PID controller (create/axes/linear+angular correction) | Supported | Supported | Supported | B |

---

## 8. Debug / Tooling

| Feature | Status | Priority | Notes |
|---|---|---|---|
| debug render | Supported | B | Native line/color buffers via `rapier_unity_debug_render` |
| gizmo drawing | Partial | B | `RapierDebugRenderer.DrawRuntimeLines` (Debug.DrawLine); editor Gizmo overlay pending |
| parity fixture runner | Supported | S | CrossHostParity sample |
| deterministic replay sample | Supported | S | DeterministicReplay sample |
| basic falling ball sample | Supported | S | BasicFallingBall sample |
| cross-host parity sample | Supported | S | CrossHostParity sample |
| native build scripts | Partial | A | `cargo build` works; packaging copy is manual |
| platform plugin packaging | Partial | A | macOS manual; other platforms not scripted |
| Unity automated tests | Missing | A | No Unity Editor CI job yet |

---

## Recommended Implementation Order

### Phase 1 — Expand RigidBody low-level API ✅ done

Implemented at the native FFI and low-level C# (`RapierWorld`) layers. Component API
wrappers are still pending.

- get / set linear velocity, angular velocity
- get / set linear damping, angular damping
- get / set gravity scale
- get / set CCD enabled
- get / set enabled
- add force, add torque
- apply impulse, apply torque impulse
- kinematic next-position: set next kinematic translation, set next kinematic rotation

### Phase 2 — Collider filtering and material API 🚧 in progress

Runtime material/filtering setters and getters (friction, restitution, combine
rules, collision/solver groups, sensor, enabled, density, translation/rotation
wrt parent) and mesh shapes (trimesh, convex hull, heightfield) are implemented
at the native FFI and low-level C# layers. Active-event flags (collision/contact
events) are deferred to Phase 4. Component-API wrappers remain pending.

- friction and restitution combine rules
- collision groups, solver groups
- active events, active collision types
- sensor flag
- local translation and rotation
- trimesh collider (foundation for mesh shapes)
- convex hull collider

### Phase 3 — Scene queries ✅ done

Filtered raycast (with normal), point projection, point intersection, multi-hit
raycast, shape casting, and shape intersection — all with full `QueryFilter`
support (flags, collision-group mask, exclude collider/body) — are implemented at
the native FFI and low-level C# layers. Shape queries support ball, cuboid, and
capsule primitives. Scene queries read the broad-phase BVH from the most recent
world step. Component-API wrappers remain pending.

- castRay variants
- castRayAndGetNormal
- projectPoint
- intersectionsWithPoint
- castShape
- intersectionsWithShape
- query filters: exclude collider/body, collision group mask

### Phase 4 — Events ✅ done

Collision (start/stop) and contact-force events are captured during each world step
via a custom `EventHandler` and drained through buffer-based FFI. Colliders opt in
with `set_active_events`; `set_contact_force_event_threshold` and
`set_active_collision_types` are also exposed. Events reflect the most recent step.
Component-API wrappers remain pending.

- collision started / stopped
- intersection started / stopped
- contact force events
- event queue drain API

### Phase 5 — Joints ✅ done

Fixed, spherical, revolute, prismatic, rope, and spring joints (impulse joints)
are implemented at the native FFI, low-level C#, and Component API layers, with
per-axis limits and position / velocity motors (target, stiffness/factor, max
force) and joint removal. Generic joints remain pending (priority C).

- fixed joint
- spherical joint
- revolute joint with limits
- prismatic joint with limits
- rope joint
- spring joint
- joint motor API
- remove joint

### Phase 6 — Character controller ✅ done

A kinematic character controller is exposed as a single stateless
`move_character` FFI: it computes collision-constrained movement for a ball /
cuboid / capsule shape given a desired translation, configurable up vector,
offset, slide, autostep, snap-to-ground, and slope angles, plus a `QueryFilter`.
It returns the effective translation and grounded / sliding flags without moving
any body. `RapierCharacterControllerComponent` wraps this for
`RapierRigidBodyComponent`, including optional next-kinematic-translation
application. Per-collision output and dynamic-body impulses remain pending.

- controller create / destroy
- compute collider movement
- computed movement and collisions output
- autostep, snap to ground, slope settings

### Phase 7 — Debug render and Unity gizmo tooling ✅ done

Rapier's debug-render pipeline (enabled via the `debug-render` crate feature) is
exposed through `rapier_unity_debug_render`, which fills caller buffers with line
endpoints and per-line colors. `RapierWorld.DebugRender` and the
`RapierDebugRenderer.DrawRuntimeLines` helper (using `Debug.DrawLine`) wrap it.
An editor-only Gizmo overlay component and a richer debug overlay remain pending
(Component-API layer).

- expose Rapier debug render vertices/colors
- Unity Gizmo draw pass in Editor
- optional debug overlay component

### Phase 8 — Component API parity & JS demos ✅ done (validation pending)

- Component layer: `RapierRigidBodyComponent` and `RapierColliderComponent` now
  wrap the Phase 1–4 runtime APIs (velocities, damping, gravity scale, CCD,
  enabled, forces/impulses, kinematic next-position, axis locks; collider
  friction/restitution/density, sensor, enabled, combine rules, collision/solver
  groups, active events/collision-types, contact-force threshold; mesh collider
  trimesh/convex-hull creation). Bodies also expose authored gravity scale,
  soft-CCD, solver iterations, dominance, per-axis locks, stable id, and
  `TryGetMass`. Colliders expose authored combine rules / groups / events /
  contact-force threshold / stable id applied on creation.
- Added shape components for the advanced cases used by the demos:
  `RapierConvexHullCollider`, `RapierTrimeshCollider`,
  `RapierHeightfieldCollider`, `RapierVoxelsCollider` (joining the existing
  box/sphere/capsule collider components). Round shapes remain unimplemented.
- `RapierPidControllerComponent` wraps the native PID controller.
- `RapierPhysics` exposes the full scene-query surface for `RapierWorldComponent`.
- `RapierWorldComponent` exposes state hashes, snapshots, and event drains.
- Joint Component wrappers cover fixed, spherical, revolute, prismatic, rope,
  and spring joints with shared lifecycle management plus limit/motor APIs.
- Samples: the `RapierComponentDemos` sample reimplements the current Rapier JS
  3D demo catalog with the component layer (each body is a GameObject with Rapier
  components), alongside the existing low-level `RapierJsDemos` sample. This is a
  practical coverage check, not a proof of complete Rapier API coverage.
- Pending: editor Gizmo overlay component and Unity Editor playmode validation of
  the ported demos.
