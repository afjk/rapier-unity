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
| take snapshot | Supported | Missing | Supported | A |
| restore snapshot | Supported | Missing | Supported | A |
| state hash (native) | Supported | Missing | Supported | S |
| canonical hash | Supported | Missing | Supported | S |
| debug render | Supported | Partial | Supported | B |
| event queue | Supported | Missing | Supported | A |
| integration parameters / solver settings | Partial | Missing | Partial | B |

---

## 2. RigidBody

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| create fixed body | Supported | Supported | Supported | S |
| create dynamic body | Supported | Supported | Supported | S |
| create kinematic position-based body | Supported | Missing | Supported | A |
| create kinematic velocity-based body | Supported | Missing | Supported | A |
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
| mass / mass properties getters | Supported | Missing | Supported | B |
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
| heightfield | Supported | Missing | Supported | B |
| round shapes | Missing | Missing | Missing | C |
| sensor | Supported | Partial | Supported | A |
| enabled | Supported | Supported | Supported | A |
| density | Supported | Supported | Supported | S |
| mass (override) | Missing | Missing | Missing | B |
| friction | Supported | Partial | Supported | S |
| restitution | Supported | Partial | Supported | S |
| friction combine rule | Supported | Supported | Supported | A |
| restitution combine rule | Supported | Supported | Supported | A |
| collision groups | Supported | Supported | Supported | A |
| solver groups | Supported | Supported | Supported | A |
| active events | Supported | Supported | Supported | A |
| active collision types | Supported | Supported | Supported | B |
| active hooks | Missing | Missing | Missing | C |
| parent body | Supported | Supported | Supported | S |
| local translation | Supported | Partial | Supported | A |
| local rotation | Supported | Partial | Supported | A |
| stable id | Supported | Supported | Supported | S |

---

## 4. Scene Queries

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| castRay | Supported | N/A | Supported | S |
| castRayAndGetNormal | Supported | N/A | Supported | A |
| intersectionsWithRay / raycast all | Supported | N/A | Supported | A |
| projectPoint | Supported | N/A | Supported | A |
| intersectionsWithPoint | Supported | N/A | Supported | B |
| castShape | Supported | N/A | Supported | A |
| intersectionsWithShape | Supported | N/A | Supported | B |
| query filters | Supported | N/A | Supported | A |
| exclude collider / body filter | Supported | N/A | Supported | A |
| collision groups filter | Supported | N/A | Supported | A |

---

## 5. Events

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| collision started | Supported | Missing | Supported | A |
| collision stopped | Supported | Missing | Supported | A |
| intersection started | Supported | Missing | Supported | A |
| intersection stopped | Supported | Missing | Supported | A |
| contact force events | Supported | Missing | Supported | B |
| contact pairs | Missing | Missing | Missing | B |
| contact manifolds | Missing | Missing | Missing | C |
| event queue drain API | Supported | Missing | Supported | A |

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

### Phase 8 — Component API parity & JS demos 🚧 in progress

- Component layer: `RapierRigidBodyComponent` and `RapierColliderComponent` now
  wrap the Phase 1–4 runtime APIs (velocities, damping, gravity scale, CCD,
  enabled, forces/impulses, kinematic next-position, axis locks; collider
  friction/restitution/density, sensor, enabled, combine rules, collision/solver
  groups, active events/collision-types, contact-force threshold; mesh collider
  trimesh/convex-hull creation).
- Joint Component wrappers now cover fixed, spherical, revolute, prismatic, rope,
  and spring joints with shared lifecycle management plus limit/motor APIs.
- Samples: the `RapierJsDemos` sample now ports the full Rapier JS 3D demo
  catalog — pyramid, keva tower, damping, CCD, fountain, collision groups,
  joints, platform, locked rotations, convex polyhedron, triangle mesh,
  heightfield, and character controller.
- Pending: editor Gizmo overlay component, query Component-layer wrappers, and
  Unity Editor playmode validation of the ported demos.
