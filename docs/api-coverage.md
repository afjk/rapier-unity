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
| debug render | Missing | Missing | Missing | B |
| event queue | Missing | Missing | Missing | A |
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
| get / set linvel | Supported | Missing | Supported | S |
| get / set angvel | Supported | Missing | Supported | S |
| get / set gravity scale | Supported | Missing | Supported | A |
| get / set linear damping | Supported | Missing | Supported | A |
| get / set angular damping | Supported | Missing | Supported | A |
| get / set additional solver iterations | Missing | Missing | Missing | B |
| get / set CCD enabled | Supported | Missing | Supported | A |
| get / set enabled | Supported | Missing | Supported | A |
| sleep / wake | Missing | Missing | Missing | B |
| add force | Supported | Missing | Supported | A |
| add torque | Supported | Missing | Supported | A |
| apply impulse | Supported | Missing | Supported | A |
| apply torque impulse | Supported | Missing | Supported | A |
| add force at point | Missing | Missing | Missing | B |
| apply impulse at point | Missing | Missing | Missing | B |
| set next kinematic translation | Supported | Missing | Supported | A |
| set next kinematic rotation | Supported | Missing | Supported | A |
| mass / mass properties getters | Missing | Missing | Missing | B |
| dominance group | Missing | Missing | Missing | C |
| user data / stable id | Supported | Supported | Supported | S |

---

## 3. Collider

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| cuboid | Supported | Supported | Supported | S |
| ball | Supported | Supported | Supported | S |
| capsule | Supported | Supported | Supported | S |
| trimesh | Supported | Missing | Supported | A |
| convex hull | Supported | Missing | Supported | A |
| heightfield | Supported | Missing | Supported | B |
| round shapes | Missing | Missing | Missing | C |
| sensor | Supported | Partial | Supported | A |
| enabled | Supported | Missing | Supported | A |
| density | Supported | Supported | Supported | S |
| mass (override) | Missing | Missing | Missing | B |
| friction | Supported | Partial | Supported | S |
| restitution | Supported | Partial | Supported | S |
| friction combine rule | Supported | Missing | Supported | A |
| restitution combine rule | Supported | Missing | Supported | A |
| collision groups | Supported | Missing | Supported | A |
| solver groups | Supported | Missing | Supported | A |
| active events | Missing | Missing | Missing | A |
| active collision types | Missing | Missing | Missing | B |
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
| collision started | Missing | Missing | Missing | A |
| collision stopped | Missing | Missing | Missing | A |
| intersection started | Missing | Missing | Missing | A |
| intersection stopped | Missing | Missing | Missing | A |
| contact force events | Missing | Missing | Missing | B |
| contact pairs | Missing | Missing | Missing | B |
| contact manifolds | Missing | Missing | Missing | C |
| event queue drain API | Missing | Missing | Missing | A |

---

## 6. Joints

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| fixed joint | Missing | Missing | Missing | A |
| spherical joint | Missing | Missing | Missing | A |
| revolute joint | Missing | Missing | Missing | A |
| prismatic joint | Missing | Missing | Missing | A |
| rope joint | Missing | Missing | Missing | B |
| spring joint | Missing | Missing | Missing | B |
| generic joint | Missing | Missing | Missing | C |
| joint motors | Missing | Missing | Missing | B |
| joint limits | Missing | Missing | Missing | A |
| remove joint | Missing | Missing | Missing | A |

---

## 7. Character Controller

| Feature | Low-level C# | Component API | Native FFI | Priority |
|---|---|---|---|---|
| create controller | Missing | Missing | Missing | A |
| compute collider movement | Missing | Missing | Missing | A |
| computed movement result | Missing | Missing | Missing | A |
| collisions output | Missing | Missing | Missing | A |
| autostep | Missing | Missing | Missing | B |
| snap to ground | Missing | Missing | Missing | B |
| slope settings | Missing | Missing | Missing | B |
| up vector | Missing | Missing | Missing | A |
| apply impulses to dynamic bodies | Missing | Missing | Missing | B |
| query filters | Missing | Missing | Missing | A |

---

## 8. Debug / Tooling

| Feature | Status | Priority | Notes |
|---|---|---|---|
| debug render | Missing | B | Rapier debug lines not yet exposed |
| gizmo drawing | Missing | B | Unity Gizmo integration not started |
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

### Phase 4 — Events

- collision started / stopped
- intersection started / stopped
- contact force events
- event queue drain API

### Phase 5 — Joints

- fixed joint
- spherical joint
- revolute joint with limits
- prismatic joint with limits
- joint motor API
- remove joint

### Phase 6 — Character controller

- controller create / destroy
- compute collider movement
- computed movement and collisions output
- autostep, snap to ground, slope settings

### Phase 7 — Debug render and Unity gizmo tooling

- expose Rapier debug render vertices/colors
- Unity Gizmo draw pass in Editor
- optional debug overlay component
