mod body;
mod collider;
mod events;
mod handles;
mod hash;
mod joints;
mod query;
mod snapshot;
mod world;

pub use body::{
    RapierUnityRigidBodyDesc, RapierUnityRigidBodyState, RapierUnityRigidBodyType,
    RapierUnityTransform, RapierUnityVector3,
};
pub use collider::{
    RapierUnityBoxColliderDesc, RapierUnityCapsuleColliderDesc, RapierUnityMeshColliderDesc,
    RapierUnitySphereColliderDesc,
};
pub use events::{RapierUnityCollisionEvent, RapierUnityContactForceEvent};
pub use handles::{RapierUnityColliderHandle, RapierUnityJointHandle, RapierUnityRigidBodyHandle};
pub use query::{
    RapierUnityPointProjection, RapierUnityQueryFilter, RapierUnityQueryShape, RapierUnityRay,
    RapierUnityRaycastHit, RapierUnityShapeCastHit,
};

/// # Safety
///
/// If `len > 0`, `bytes` must be valid for reads of `len` bytes.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_stable_id_hash(bytes: *const u8, len: usize) -> u64 {
    if len == 0 {
        return 0;
    }

    if bytes.is_null() {
        return 0;
    }

    let bytes = unsafe { std::slice::from_raw_parts(bytes, len) };
    hash::stable_id_hash_bytes(bytes)
}

/// Writes a produced value through `out` when it exists and the pointer is non-null.
///
/// # Safety
///
/// `out` must be valid for writes of one `T` when it is non-null.
unsafe fn write_value<T>(out: *mut T, produce: impl FnOnce() -> Option<T>) -> bool {
    if out.is_null() {
        return false;
    }

    if let Some(value) = produce() {
        unsafe {
            *out = value;
        }
        true
    } else {
        false
    }
}

/// Builds a slice from a raw pointer/length pair.
///
/// Returns `Some(&[])` for a zero length (ignoring the pointer) and `None` when
/// the pointer is null but a non-zero length was requested.
///
/// # Safety
///
/// When `len > 0`, `ptr` must be valid for reads of `len` elements of `T`.
unsafe fn raw_slice<'a, T>(ptr: *const T, len: usize) -> Option<&'a [T]> {
    if len == 0 {
        return Some(&[]);
    }

    if ptr.is_null() {
        return None;
    }

    Some(unsafe { std::slice::from_raw_parts(ptr, len) })
}

/// Builds a mutable slice from a raw pointer/length pair.
///
/// Returns `Some(&mut [])` for a zero length (ignoring the pointer) and `None`
/// when the pointer is null but a non-zero length was requested.
///
/// # Safety
///
/// When `len > 0`, `ptr` must be valid for writes of `len` elements of `T` and
/// must not alias any other live reference.
unsafe fn raw_slice_mut<'a, T>(ptr: *mut T, len: usize) -> Option<&'a mut [T]> {
    if len == 0 {
        return Some(&mut []);
    }

    if ptr.is_null() {
        return None;
    }

    Some(unsafe { std::slice::from_raw_parts_mut(ptr, len) })
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_create() -> u64 {
    world::create_world()
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_destroy(world_id: u64) -> bool {
    world::destroy_world(world_id)
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_set_gravity(world_id: u64, x: f32, y: f32, z: f32) -> bool {
    world::with_world_mut(world_id, |world| world.set_gravity(x, y, z)).is_some()
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_set_timestep(world_id: u64, dt: f32) -> bool {
    world::with_world_mut(world_id, |world| world.set_timestep(dt)).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_step(world_id: u64) -> bool {
    world::with_world_mut(world_id, |world| world.step()).is_some()
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_create(
    world_id: u64,
    desc: RapierUnityRigidBodyDesc,
) -> RapierUnityRigidBodyHandle {
    world::with_world_mut(world_id, |world| body::create_body(world, desc))
        .unwrap_or(RapierUnityRigidBodyHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_destroy(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
) -> bool {
    world::with_world_mut(world_id, |world| body::destroy_body(world, body)).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_stable_id(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    stable_id: u64,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_stable_id(world, body, stable_id)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_transform` must be valid for writes of one `RapierUnityTransform`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_transform(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_transform: *mut RapierUnityTransform,
) -> bool {
    if out_transform.is_null() {
        return false;
    }

    let transform =
        world::with_world(world_id, |world| body::get_body_transform(world, body)).flatten();

    if let Some(transform) = transform {
        unsafe {
            *out_transform = transform;
        }
        true
    } else {
        false
    }
}

/// # Safety
///
/// `out_state` must be valid for writes of one `RapierUnityRigidBodyState`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_state(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_state: *mut RapierUnityRigidBodyState,
) -> bool {
    if out_state.is_null() {
        return false;
    }

    let state = world::with_world(world_id, |world| body::get_body_state(world, body)).flatten();

    if let Some(state) = state {
        unsafe {
            *out_state = state;
        }
        true
    } else {
        false
    }
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_transform(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    transform: RapierUnityTransform,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_transform(world, body, transform)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_velocity` must be valid for writes of one `RapierUnityVector3`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_linvel(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_velocity: *mut RapierUnityVector3,
) -> bool {
    write_value(out_velocity, || {
        world::with_world(world_id, |world| body::get_body_linvel(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_linvel(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    velocity: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_linvel(world, body, velocity, wake_up)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_velocity` must be valid for writes of one `RapierUnityVector3`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_angvel(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_velocity: *mut RapierUnityVector3,
) -> bool {
    write_value(out_velocity, || {
        world::with_world(world_id, |world| body::get_body_angvel(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_angvel(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    velocity: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_angvel(world, body, velocity, wake_up)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_damping` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_linear_damping(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_damping: *mut f32,
) -> bool {
    write_value(out_damping, || {
        world::with_world(world_id, |world| body::get_body_linear_damping(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_linear_damping(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    damping: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_linear_damping(world, body, damping)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_damping` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_angular_damping(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_damping: *mut f32,
) -> bool {
    write_value(out_damping, || {
        world::with_world(world_id, |world| {
            body::get_body_angular_damping(world, body)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_angular_damping(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    damping: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_angular_damping(world, body, damping)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_scale` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_gravity_scale(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_scale: *mut f32,
) -> bool {
    write_value(out_scale, || {
        world::with_world(world_id, |world| body::get_body_gravity_scale(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_gravity_scale(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    scale: f32,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_gravity_scale(world, body, scale, wake_up)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_enabled` must be valid for writes of one `bool`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_ccd_enabled(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_enabled: *mut bool,
) -> bool {
    write_value(out_enabled, || {
        world::with_world(world_id, |world| body::get_body_ccd_enabled(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_ccd_enabled(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    enabled: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_ccd_enabled(world, body, enabled)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_enabled` must be valid for writes of one `bool`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_body_get_enabled(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    out_enabled: *mut bool,
) -> bool {
    write_value(out_enabled, || {
        world::with_world(world_id, |world| body::get_body_enabled(world, body)).flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_enabled(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    enabled: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_enabled(world, body, enabled)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_add_force(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    force: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::add_body_force(world, body, force, wake_up)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_add_torque(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    torque: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::add_body_torque(world, body, torque, wake_up)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_apply_impulse(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    impulse: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::apply_body_impulse(world, body, impulse, wake_up)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_apply_torque_impulse(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    impulse: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::apply_body_torque_impulse(world, body, impulse, wake_up)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_next_kinematic_translation(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    translation: RapierUnityVector3,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_next_kinematic_translation(world, body, translation)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_body_set_next_kinematic_rotation(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    rotation: RapierUnityTransform,
) -> bool {
    world::with_world_mut(world_id, |world| {
        body::set_body_next_kinematic_rotation(world, body, rotation)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_create_box(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnityBoxColliderDesc,
) -> RapierUnityColliderHandle {
    world::with_world_mut(world_id, |world| {
        collider::create_box_collider(world, body, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_create_sphere(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnitySphereColliderDesc,
) -> RapierUnityColliderHandle {
    world::with_world_mut(world_id, |world| {
        collider::create_sphere_collider(world, body, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_create_capsule(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnityCapsuleColliderDesc,
) -> RapierUnityColliderHandle {
    world::with_world_mut(world_id, |world| {
        collider::create_capsule_collider(world, body, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

/// # Safety
///
/// `vertices` must be valid for reads of `vertex_count * 3` `f32` values and
/// `indices` valid for reads of `index_count` `u32` values (when their length
/// is non-zero).
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_create_trimesh(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    vertices: *const f32,
    vertex_count: usize,
    indices: *const u32,
    index_count: usize,
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    let vertices = unsafe { raw_slice(vertices, vertex_count.saturating_mul(3)) };
    let indices = unsafe { raw_slice(indices, index_count) };
    let (Some(vertices), Some(indices)) = (vertices, indices) else {
        return RapierUnityColliderHandle::INVALID;
    };

    world::with_world_mut(world_id, |world| {
        collider::create_trimesh_collider(world, body, vertices, indices, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

/// # Safety
///
/// `vertices` must be valid for reads of `vertex_count * 3` `f32` values (when
/// `vertex_count` is non-zero).
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_create_convex_hull(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    vertices: *const f32,
    vertex_count: usize,
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    let Some(vertices) = (unsafe { raw_slice(vertices, vertex_count.saturating_mul(3)) }) else {
        return RapierUnityColliderHandle::INVALID;
    };

    world::with_world_mut(world_id, |world| {
        collider::create_convex_hull_collider(world, body, vertices, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

/// # Safety
///
/// `heights` must be valid for reads of `rows * columns` `f32` values (when
/// that product is non-zero). Heights are interpreted in row-major order.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_create_heightfield(
    world_id: u64,
    body: RapierUnityRigidBodyHandle,
    heights: *const f32,
    rows: usize,
    columns: usize,
    scale: RapierUnityVector3,
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    let Some(heights) = (unsafe { raw_slice(heights, rows.saturating_mul(columns)) }) else {
        return RapierUnityColliderHandle::INVALID;
    };

    world::with_world_mut(world_id, |world| {
        collider::create_heightfield_collider(world, body, heights, rows, columns, scale, desc)
    })
    .unwrap_or(RapierUnityColliderHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_destroy(
    world_id: u64,
    collider: RapierUnityColliderHandle,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::destroy_collider(world, collider)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_stable_id(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    stable_id: u64,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_stable_id(world, collider, stable_id)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_friction` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_friction(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_friction: *mut f32,
) -> bool {
    write_value(out_friction, || {
        world::with_world(world_id, |world| {
            collider::get_collider_friction(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_friction(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    friction: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_friction(world, collider, friction)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_restitution` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_restitution(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_restitution: *mut f32,
) -> bool {
    write_value(out_restitution, || {
        world::with_world(world_id, |world| {
            collider::get_collider_restitution(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_restitution(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    restitution: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_restitution(world, collider, restitution)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_rule` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_friction_combine_rule(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_rule: *mut u32,
) -> bool {
    write_value(out_rule, || {
        world::with_world(world_id, |world| {
            collider::get_collider_friction_combine_rule(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_friction_combine_rule(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    rule: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_friction_combine_rule(world, collider, rule)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_rule` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_restitution_combine_rule(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_rule: *mut u32,
) -> bool {
    write_value(out_rule, || {
        world::with_world(world_id, |world| {
            collider::get_collider_restitution_combine_rule(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_restitution_combine_rule(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    rule: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_restitution_combine_rule(world, collider, rule)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_groups` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_collision_groups(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_groups: *mut u32,
) -> bool {
    write_value(out_groups, || {
        world::with_world(world_id, |world| {
            collider::get_collider_collision_groups(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_collision_groups(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    groups: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_collision_groups(world, collider, groups)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_groups` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_solver_groups(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_groups: *mut u32,
) -> bool {
    write_value(out_groups, || {
        world::with_world(world_id, |world| {
            collider::get_collider_solver_groups(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_solver_groups(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    groups: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_solver_groups(world, collider, groups)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_sensor` must be valid for writes of one `bool`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_sensor(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_sensor: *mut bool,
) -> bool {
    write_value(out_sensor, || {
        world::with_world(world_id, |world| {
            collider::get_collider_sensor(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_sensor(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    is_sensor: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_sensor(world, collider, is_sensor)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_enabled` must be valid for writes of one `bool`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_enabled(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_enabled: *mut bool,
) -> bool {
    write_value(out_enabled, || {
        world::with_world(world_id, |world| {
            collider::get_collider_enabled(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_enabled(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    enabled: bool,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_enabled(world, collider, enabled)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_density` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_density(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_density: *mut f32,
) -> bool {
    write_value(out_density, || {
        world::with_world(world_id, |world| {
            collider::get_collider_density(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_density(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    density: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_density(world, collider, density)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_translation_wrt_parent(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    translation: RapierUnityVector3,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_translation_wrt_parent(world, collider, translation)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_position_wrt_parent(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    transform: RapierUnityTransform,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_position_wrt_parent(world, collider, transform)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_flags` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_active_events(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_flags: *mut u32,
) -> bool {
    write_value(out_flags, || {
        world::with_world(world_id, |world| {
            collider::get_collider_active_events(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_active_events(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    flags: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_active_events(world, collider, flags)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_types` must be valid for writes of one `u32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_active_collision_types(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_types: *mut u32,
) -> bool {
    write_value(out_types, || {
        world::with_world(world_id, |world| {
            collider::get_collider_active_collision_types(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_active_collision_types(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    types: u32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_active_collision_types(world, collider, types)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_threshold` must be valid for writes of one `f32`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_collider_get_contact_force_event_threshold(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    out_threshold: *mut f32,
) -> bool {
    write_value(out_threshold, || {
        world::with_world(world_id, |world| {
            collider::get_collider_contact_force_event_threshold(world, collider)
        })
        .flatten()
    })
}

#[no_mangle]
pub extern "C" fn rapier_unity_collider_set_contact_force_event_threshold(
    world_id: u64,
    collider: RapierUnityColliderHandle,
    threshold: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        collider::set_collider_contact_force_event_threshold(world, collider, threshold)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_events` must be valid for writes of `max_events` `RapierUnityCollisionEvent`
/// values when `max_events` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_drain_collision_events(
    world_id: u64,
    out_events: *mut RapierUnityCollisionEvent,
    max_events: usize,
) -> usize {
    let Some(out) = (unsafe { raw_slice_mut(out_events, max_events) }) else {
        return 0;
    };

    world::with_world(world_id, |world| events::drain_collision_events(world, out)).unwrap_or(0)
}

/// # Safety
///
/// `out_events` must be valid for writes of `max_events`
/// `RapierUnityContactForceEvent` values when `max_events` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_drain_contact_force_events(
    world_id: u64,
    out_events: *mut RapierUnityContactForceEvent,
    max_events: usize,
) -> usize {
    let Some(out) = (unsafe { raw_slice_mut(out_events, max_events) }) else {
        return 0;
    };

    world::with_world(world_id, |world| {
        events::drain_contact_force_events(world, out)
    })
    .unwrap_or(0)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_create_fixed(
    world_id: u64,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
) -> RapierUnityJointHandle {
    world::with_world_mut(world_id, |world| {
        joints::create_fixed_joint(world, body1, body2, anchor1, anchor2)
    })
    .unwrap_or(RapierUnityJointHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_create_spherical(
    world_id: u64,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
) -> RapierUnityJointHandle {
    world::with_world_mut(world_id, |world| {
        joints::create_spherical_joint(world, body1, body2, anchor1, anchor2)
    })
    .unwrap_or(RapierUnityJointHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_create_revolute(
    world_id: u64,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    axis: RapierUnityVector3,
) -> RapierUnityJointHandle {
    world::with_world_mut(world_id, |world| {
        joints::create_revolute_joint(world, body1, body2, anchor1, anchor2, axis)
    })
    .unwrap_or(RapierUnityJointHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_create_prismatic(
    world_id: u64,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    axis: RapierUnityVector3,
) -> RapierUnityJointHandle {
    world::with_world_mut(world_id, |world| {
        joints::create_prismatic_joint(world, body1, body2, anchor1, anchor2, axis)
    })
    .unwrap_or(RapierUnityJointHandle::INVALID)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_remove(world_id: u64, joint: RapierUnityJointHandle) -> bool {
    world::with_world_mut(world_id, |world| joints::remove_joint(world, joint)).unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_set_limits(
    world_id: u64,
    joint: RapierUnityJointHandle,
    axis: u32,
    min: f32,
    max: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        joints::set_joint_limits(world, joint, axis, min, max)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_set_motor_position(
    world_id: u64,
    joint: RapierUnityJointHandle,
    axis: u32,
    target_position: f32,
    stiffness: f32,
    damping: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        joints::set_joint_motor_position(world, joint, axis, target_position, stiffness, damping)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_set_motor_velocity(
    world_id: u64,
    joint: RapierUnityJointHandle,
    axis: u32,
    target_velocity: f32,
    factor: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        joints::set_joint_motor_velocity(world, joint, axis, target_velocity, factor)
    })
    .unwrap_or(false)
}

#[no_mangle]
pub extern "C" fn rapier_unity_joint_set_motor_max_force(
    world_id: u64,
    joint: RapierUnityJointHandle,
    axis: u32,
    max_force: f32,
) -> bool {
    world::with_world_mut(world_id, |world| {
        joints::set_joint_motor_max_force(world, joint, axis, max_force)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// `out_hit` must be valid for writes of one `RapierUnityRaycastHit`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_raycast(
    world_id: u64,
    ray: RapierUnityRay,
    max_toi: f32,
    out_hit: *mut RapierUnityRaycastHit,
) -> bool {
    if out_hit.is_null() {
        return false;
    }

    let hit = world::with_world(world_id, |world| query::raycast(world, ray, max_toi)).flatten();

    if let Some(hit) = hit {
        unsafe {
            *out_hit = hit;
        }
        true
    } else {
        false
    }
}

/// # Safety
///
/// `out_hit` must be valid for writes of one `RapierUnityRaycastHit`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_raycast_filtered(
    world_id: u64,
    ray: RapierUnityRay,
    max_toi: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
    out_hit: *mut RapierUnityRaycastHit,
) -> bool {
    write_value(out_hit, || {
        world::with_world(world_id, |world| {
            query::raycast_filtered(world, ray, max_toi, solid, filter)
        })
        .flatten()
    })
}

/// # Safety
///
/// `out_projection` must be valid for writes of one `RapierUnityPointProjection`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_project_point(
    world_id: u64,
    point_x: f32,
    point_y: f32,
    point_z: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
    out_projection: *mut RapierUnityPointProjection,
) -> bool {
    write_value(out_projection, || {
        world::with_world(world_id, |world| {
            query::project_point(world, point_x, point_y, point_z, solid, filter)
        })
        .flatten()
    })
}

/// # Safety
///
/// `out_collider` must be valid for writes of one `RapierUnityColliderHandle`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_intersection_with_point(
    world_id: u64,
    point_x: f32,
    point_y: f32,
    point_z: f32,
    filter: RapierUnityQueryFilter,
    out_collider: *mut RapierUnityColliderHandle,
) -> bool {
    write_value(out_collider, || {
        world::with_world(world_id, |world| {
            query::intersection_with_point(world, point_x, point_y, point_z, filter)
        })
        .flatten()
    })
}

/// # Safety
///
/// `out_hits` must be valid for writes of `max_hits` `RapierUnityRaycastHit`
/// values when `max_hits` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_raycast_all(
    world_id: u64,
    ray: RapierUnityRay,
    max_toi: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
    out_hits: *mut RapierUnityRaycastHit,
    max_hits: usize,
) -> usize {
    let Some(out) = (unsafe { raw_slice_mut(out_hits, max_hits) }) else {
        return 0;
    };

    world::with_world(world_id, |world| {
        query::raycast_all(world, ray, max_toi, solid, filter, out)
    })
    .unwrap_or(0)
}

/// # Safety
///
/// `out_hit` must be valid for writes of one `RapierUnityShapeCastHit`.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_cast_shape(
    world_id: u64,
    shape_pos: RapierUnityTransform,
    shape_vel: RapierUnityVector3,
    shape: RapierUnityQueryShape,
    max_toi: f32,
    stop_at_penetration: bool,
    filter: RapierUnityQueryFilter,
    out_hit: *mut RapierUnityShapeCastHit,
) -> bool {
    write_value(out_hit, || {
        world::with_world(world_id, |world| {
            query::cast_shape(
                world,
                shape_pos,
                shape_vel,
                shape,
                max_toi,
                stop_at_penetration,
                filter,
            )
        })
        .flatten()
    })
}

/// # Safety
///
/// `out_colliders` must be valid for writes of `max_colliders`
/// `RapierUnityColliderHandle` values when `max_colliders` is non-zero.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_intersect_shape(
    world_id: u64,
    shape_pos: RapierUnityTransform,
    shape: RapierUnityQueryShape,
    filter: RapierUnityQueryFilter,
    out_colliders: *mut RapierUnityColliderHandle,
    max_colliders: usize,
) -> usize {
    let Some(out) = (unsafe { raw_slice_mut(out_colliders, max_colliders) }) else {
        return 0;
    };

    world::with_world(world_id, |world| {
        query::intersect_shape(world, shape_pos, shape, filter, out)
    })
    .unwrap_or(0)
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_state_hash(world_id: u64) -> u64 {
    world::with_world(world_id, hash::world_state_hash).unwrap_or(0)
}

#[no_mangle]
pub extern "C" fn rapier_unity_world_snapshot_size(world_id: u64) -> usize {
    world::with_world(world_id, snapshot::snapshot_size).unwrap_or(0)
}

/// # Safety
///
/// If `len > 0`, `out_bytes` must be valid for writes of `len` bytes.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_world_snapshot_write(
    world_id: u64,
    out_bytes: *mut u8,
    len: usize,
) -> bool {
    world::with_world(world_id, |world| {
        snapshot::snapshot_write(world, out_bytes, len)
    })
    .unwrap_or(false)
}

/// # Safety
///
/// If `len > 0`, `bytes` must be valid for reads of `len` bytes.
#[no_mangle]
pub unsafe extern "C" fn rapier_unity_world_snapshot_read(
    world_id: u64,
    bytes: *const u8,
    len: usize,
) -> bool {
    world::with_world_mut(world_id, |world| snapshot::snapshot_read(world, bytes, len))
        .unwrap_or(false)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn create_test_body(world_id: u64) -> RapierUnityRigidBodyHandle {
        rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_y: 10.0,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        )
    }

    fn attach_test_box(
        world_id: u64,
        body: RapierUnityRigidBodyHandle,
    ) -> RapierUnityColliderHandle {
        rapier_unity_collider_create_box(world_id, body, RapierUnityBoxColliderDesc::default())
    }

    fn create_stable_test_body(
        world_id: u64,
        stable_id: u64,
        y: f32,
    ) -> (RapierUnityRigidBodyHandle, RapierUnityColliderHandle) {
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_y: y,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(body.is_valid());
        assert!(rapier_unity_body_set_stable_id(world_id, body, stable_id));

        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());
        assert!(rapier_unity_collider_set_stable_id(
            world_id, collider, stable_id
        ));

        (body, collider)
    }

    #[test]
    fn world_create_destroy() {
        let world_id = rapier_unity_world_create();
        assert_ne!(world_id, 0);
        assert!(rapier_unity_world_destroy(world_id));
        assert!(!rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn step_with_gravity_moves_dynamic_body() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_gravity(world_id, 0.0, -9.81, 0.0));
        assert!(rapier_unity_world_set_timestep(world_id, 1.0 / 60.0));

        let body = create_test_body(world_id);
        assert!(body.is_valid());
        assert!(attach_test_box(world_id, body).is_valid());

        assert!(rapier_unity_world_step(world_id));

        let mut transform = RapierUnityTransform::default();
        assert!(unsafe { rapier_unity_body_get_transform(world_id, body, &mut transform) });
        assert!(transform.position_y < 10.0);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn create_dynamic_body_and_attach_box_collider() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        assert!(body.is_valid());

        let collider =
            rapier_unity_collider_create_box(world_id, body, RapierUnityBoxColliderDesc::default());
        assert!(collider.is_valid());

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn body_state_reports_pose_and_velocities() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_x: -0.75,
                position_y: 5.0,
                linear_velocity_x: 0.75,
                linear_velocity_z: 0.15,
                angular_velocity_x: 0.35,
                angular_velocity_y: 1.25,
                angular_velocity_z: 0.55,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(body.is_valid());

        let mut state = RapierUnityRigidBodyState {
            transform: RapierUnityTransform::default(),
            linear_velocity_x: 0.0,
            linear_velocity_y: 0.0,
            linear_velocity_z: 0.0,
            angular_velocity_x: 0.0,
            angular_velocity_y: 0.0,
            angular_velocity_z: 0.0,
            sleeping: 0,
            enabled: 0,
        };
        assert!(unsafe { rapier_unity_body_get_state(world_id, body, &mut state) });
        assert_eq!(state.transform.position_x, -0.75);
        assert_eq!(state.transform.position_y, 5.0);
        assert_eq!(state.linear_velocity_x, 0.75);
        assert_eq!(state.linear_velocity_z, 0.15);
        assert_eq!(state.angular_velocity_y, 1.25);
        assert_eq!(state.enabled, 1);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collider_material_fields_affect_state_hash() {
        let world_a = rapier_unity_world_create();
        let world_b = rapier_unity_world_create();

        let body_a = create_test_body(world_a);
        let body_b = create_test_body(world_b);
        assert!(rapier_unity_body_set_stable_id(world_a, body_a, 10));
        assert!(rapier_unity_body_set_stable_id(world_b, body_b, 10));

        let collider_a = rapier_unity_collider_create_box(
            world_a,
            body_a,
            RapierUnityBoxColliderDesc {
                friction: 0.5,
                restitution: 0.0,
                ..RapierUnityBoxColliderDesc::default()
            },
        );
        let collider_b = rapier_unity_collider_create_box(
            world_b,
            body_b,
            RapierUnityBoxColliderDesc {
                friction: 0.5,
                restitution: 0.2,
                ..RapierUnityBoxColliderDesc::default()
            },
        );
        assert!(rapier_unity_collider_set_stable_id(world_a, collider_a, 10));
        assert!(rapier_unity_collider_set_stable_id(world_b, collider_b, 10));

        assert_ne!(
            rapier_unity_world_state_hash(world_a),
            rapier_unity_world_state_hash(world_b)
        );

        assert!(rapier_unity_world_destroy(world_a));
        assert!(rapier_unity_world_destroy(world_b));
    }

    #[test]
    fn state_hash_changes_after_stepping() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        assert!(body.is_valid());
        assert!(attach_test_box(world_id, body).is_valid());

        let before = rapier_unity_world_state_hash(world_id);
        assert!(rapier_unity_world_step(world_id));
        let after = rapier_unity_world_state_hash(world_id);

        assert_ne!(before, after);
        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn identical_worlds_produce_same_hash_after_same_steps() {
        let world_a = rapier_unity_world_create();
        let world_b = rapier_unity_world_create();

        let body_a = create_test_body(world_a);
        let body_b = create_test_body(world_b);
        assert!(body_a.is_valid());
        assert!(body_b.is_valid());
        assert!(attach_test_box(world_a, body_a).is_valid());
        assert!(attach_test_box(world_b, body_b).is_valid());

        for _ in 0..120 {
            assert!(rapier_unity_world_step(world_a));
            assert!(rapier_unity_world_step(world_b));
        }

        assert_eq!(
            rapier_unity_world_state_hash(world_a),
            rapier_unity_world_state_hash(world_b)
        );

        assert!(rapier_unity_world_destroy(world_a));
        assert!(rapier_unity_world_destroy(world_b));
    }

    #[test]
    fn stable_ids_make_initial_hash_independent_of_creation_order() {
        let world_a = rapier_unity_world_create();
        let world_b = rapier_unity_world_create();

        create_stable_test_body(world_a, 10, 8.0);
        create_stable_test_body(world_a, 20, 12.0);

        create_stable_test_body(world_b, 20, 12.0);
        create_stable_test_body(world_b, 10, 8.0);

        assert_eq!(
            rapier_unity_world_state_hash(world_a),
            rapier_unity_world_state_hash(world_b)
        );

        assert!(rapier_unity_world_destroy(world_a));
        assert!(rapier_unity_world_destroy(world_b));
    }

    #[test]
    fn body_step_settings_affect_state_hash() {
        let world_a = rapier_unity_world_create();
        let world_b = rapier_unity_world_create();
        let world_c = rapier_unity_world_create();

        let body_a = rapier_unity_body_create(
            world_a,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                can_sleep: 1,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let body_b = rapier_unity_body_create(
            world_b,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                linear_damping: 0.25,
                angular_damping: 0.5,
                can_sleep: 1,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let body_c = rapier_unity_body_create(
            world_c,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                can_sleep: 0,
                ccd_enabled: 1,
                ..RapierUnityRigidBodyDesc::default()
            },
        );

        assert!(rapier_unity_body_set_stable_id(world_a, body_a, 10));
        assert!(rapier_unity_body_set_stable_id(world_b, body_b, 10));
        assert!(rapier_unity_body_set_stable_id(world_c, body_c, 10));
        assert!(attach_test_box(world_a, body_a).is_valid());
        assert!(attach_test_box(world_b, body_b).is_valid());
        assert!(attach_test_box(world_c, body_c).is_valid());

        assert_ne!(
            rapier_unity_world_state_hash(world_a),
            rapier_unity_world_state_hash(world_b)
        );
        assert_ne!(
            rapier_unity_world_state_hash(world_a),
            rapier_unity_world_state_hash(world_c)
        );

        assert!(rapier_unity_world_destroy(world_a));
        assert!(rapier_unity_world_destroy(world_b));
        assert!(rapier_unity_world_destroy(world_c));
    }

    #[test]
    fn duplicate_stable_ids_are_rejected() {
        let world_id = rapier_unity_world_create();
        let body_a = create_test_body(world_id);
        let body_b = create_test_body(world_id);
        assert!(rapier_unity_body_set_stable_id(world_id, body_a, 10));
        assert!(rapier_unity_body_set_stable_id(world_id, body_a, 10));
        assert!(!rapier_unity_body_set_stable_id(world_id, body_b, 10));

        let collider_a = attach_test_box(world_id, body_a);
        let collider_b = attach_test_box(world_id, body_b);
        assert!(rapier_unity_collider_set_stable_id(
            world_id, collider_a, 20
        ));
        assert!(rapier_unity_collider_set_stable_id(
            world_id, collider_a, 20
        ));
        assert!(!rapier_unity_collider_set_stable_id(
            world_id, collider_b, 20
        ));

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn stable_id_hash_is_available_for_scene_sync_object_ids() {
        let id = b"scene-sync-object-1";
        assert_eq!(
            unsafe { rapier_unity_stable_id_hash(id.as_ptr(), id.len()) },
            hash::stable_id_hash_bytes(id)
        );
        assert_eq!(unsafe { rapier_unity_stable_id_hash(id.as_ptr(), 0) }, 0);
    }

    #[test]
    fn linear_and_angular_velocity_roundtrip() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        assert!(body.is_valid());

        assert!(rapier_unity_body_set_linvel(
            world_id,
            body,
            RapierUnityVector3 {
                x: 1.0,
                y: -2.0,
                z: 3.0,
            },
            true,
        ));
        assert!(rapier_unity_body_set_angvel(
            world_id,
            body,
            RapierUnityVector3 {
                x: 0.25,
                y: 0.5,
                z: -0.75,
            },
            true,
        ));

        let mut linvel = RapierUnityVector3::default();
        let mut angvel = RapierUnityVector3::default();
        assert!(unsafe { rapier_unity_body_get_linvel(world_id, body, &mut linvel) });
        assert!(unsafe { rapier_unity_body_get_angvel(world_id, body, &mut angvel) });
        assert_eq!(linvel.x, 1.0);
        assert_eq!(linvel.y, -2.0);
        assert_eq!(linvel.z, 3.0);
        assert_eq!(angvel.x, 0.25);
        assert_eq!(angvel.y, 0.5);
        assert_eq!(angvel.z, -0.75);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn apply_impulse_changes_linear_velocity() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_gravity(world_id, 0.0, 0.0, 0.0));
        let body = create_test_body(world_id);
        assert!(attach_test_box(world_id, body).is_valid());

        assert!(rapier_unity_body_apply_impulse(
            world_id,
            body,
            RapierUnityVector3 {
                x: 5.0,
                y: 0.0,
                z: 0.0,
            },
            true,
        ));
        assert!(rapier_unity_world_step(world_id));

        let mut linvel = RapierUnityVector3::default();
        assert!(unsafe { rapier_unity_body_get_linvel(world_id, body, &mut linvel) });
        assert!(linvel.x > 0.0);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn add_force_moves_body_against_gravity() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_gravity(world_id, 0.0, -9.81, 0.0));
        assert!(rapier_unity_world_set_timestep(world_id, 1.0 / 60.0));
        let body = create_test_body(world_id);
        assert!(attach_test_box(world_id, body).is_valid());

        // A large upward force should overcome gravity over several steps.
        for _ in 0..30 {
            assert!(rapier_unity_body_add_force(
                world_id,
                body,
                RapierUnityVector3 {
                    x: 0.0,
                    y: 200.0,
                    z: 0.0,
                },
                true,
            ));
            assert!(rapier_unity_world_step(world_id));
        }

        let mut linvel = RapierUnityVector3::default();
        assert!(unsafe { rapier_unity_body_get_linvel(world_id, body, &mut linvel) });
        assert!(linvel.y > 0.0);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn body_property_setters_roundtrip() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);

        assert!(rapier_unity_body_set_linear_damping(world_id, body, 0.5));
        assert!(rapier_unity_body_set_angular_damping(world_id, body, 0.25));
        assert!(rapier_unity_body_set_gravity_scale(
            world_id, body, 2.0, true
        ));
        assert!(rapier_unity_body_set_ccd_enabled(world_id, body, true));
        assert!(rapier_unity_body_set_enabled(world_id, body, false));

        let mut linear_damping = 0.0_f32;
        let mut angular_damping = 0.0_f32;
        let mut gravity_scale = 0.0_f32;
        let mut ccd_enabled = false;
        let mut enabled = true;
        assert!(unsafe {
            rapier_unity_body_get_linear_damping(world_id, body, &mut linear_damping)
        });
        assert!(unsafe {
            rapier_unity_body_get_angular_damping(world_id, body, &mut angular_damping)
        });
        assert!(unsafe { rapier_unity_body_get_gravity_scale(world_id, body, &mut gravity_scale) });
        assert!(unsafe { rapier_unity_body_get_ccd_enabled(world_id, body, &mut ccd_enabled) });
        assert!(unsafe { rapier_unity_body_get_enabled(world_id, body, &mut enabled) });

        assert_eq!(linear_damping, 0.5);
        assert_eq!(angular_damping, 0.25);
        assert_eq!(gravity_scale, 2.0);
        assert!(ccd_enabled);
        assert!(!enabled);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn kinematic_body_follows_next_translation() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_timestep(world_id, 1.0 / 60.0));
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::KinematicPositionBased as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(body.is_valid());

        assert!(rapier_unity_body_set_next_kinematic_translation(
            world_id,
            body,
            RapierUnityVector3 {
                x: 4.0,
                y: 0.0,
                z: 0.0,
            },
        ));
        assert!(rapier_unity_world_step(world_id));

        let mut transform = RapierUnityTransform::default();
        assert!(unsafe { rapier_unity_body_get_transform(world_id, body, &mut transform) });
        assert!((transform.position_x - 4.0).abs() < 1e-4);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn missing_body_getters_return_false() {
        let world_id = rapier_unity_world_create();
        let missing = RapierUnityRigidBodyHandle::INVALID;

        let mut linvel = RapierUnityVector3::default();
        assert!(!unsafe { rapier_unity_body_get_linvel(world_id, missing, &mut linvel) });
        assert!(!rapier_unity_body_set_linvel(
            world_id,
            missing,
            RapierUnityVector3::default(),
            true
        ));

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collider_material_setters_roundtrip() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());

        assert!(rapier_unity_collider_set_friction(world_id, collider, 0.8));
        assert!(rapier_unity_collider_set_restitution(
            world_id, collider, 0.3
        ));
        assert!(rapier_unity_collider_set_density(world_id, collider, 2.5));
        assert!(rapier_unity_collider_set_friction_combine_rule(
            world_id, collider, 3
        ));
        assert!(rapier_unity_collider_set_restitution_combine_rule(
            world_id, collider, 1
        ));

        let mut friction = 0.0_f32;
        let mut restitution = 0.0_f32;
        let mut density = 0.0_f32;
        let mut friction_rule = 0_u32;
        let mut restitution_rule = 0_u32;
        assert!(unsafe { rapier_unity_collider_get_friction(world_id, collider, &mut friction) });
        assert!(unsafe {
            rapier_unity_collider_get_restitution(world_id, collider, &mut restitution)
        });
        assert!(unsafe { rapier_unity_collider_get_density(world_id, collider, &mut density) });
        assert!(unsafe {
            rapier_unity_collider_get_friction_combine_rule(world_id, collider, &mut friction_rule)
        });
        assert!(unsafe {
            rapier_unity_collider_get_restitution_combine_rule(
                world_id,
                collider,
                &mut restitution_rule,
            )
        });

        assert_eq!(friction, 0.8);
        assert_eq!(restitution, 0.3);
        assert_eq!(density, 2.5);
        assert_eq!(friction_rule, 3);
        assert_eq!(restitution_rule, 1);

        // Out-of-range combine rules are rejected without mutating state.
        assert!(!rapier_unity_collider_set_friction_combine_rule(
            world_id, collider, 99
        ));

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collider_filtering_and_flags_roundtrip() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());

        // Memberships in the high 16 bits, filter in the low 16 bits.
        let groups = (0x0005_u32 << 16) | 0x00FF_u32;
        assert!(rapier_unity_collider_set_collision_groups(
            world_id, collider, groups
        ));
        assert!(rapier_unity_collider_set_solver_groups(
            world_id, collider, groups
        ));
        assert!(rapier_unity_collider_set_sensor(world_id, collider, true));
        assert!(rapier_unity_collider_set_enabled(world_id, collider, false));

        let mut collision_groups = 0_u32;
        let mut solver_groups = 0_u32;
        let mut sensor = false;
        let mut enabled = true;
        assert!(unsafe {
            rapier_unity_collider_get_collision_groups(world_id, collider, &mut collision_groups)
        });
        assert!(unsafe {
            rapier_unity_collider_get_solver_groups(world_id, collider, &mut solver_groups)
        });
        assert!(unsafe { rapier_unity_collider_get_sensor(world_id, collider, &mut sensor) });
        assert!(unsafe { rapier_unity_collider_get_enabled(world_id, collider, &mut enabled) });

        assert_eq!(collision_groups, groups);
        assert_eq!(solver_groups, groups);
        assert!(sensor);
        assert!(!enabled);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collider_position_wrt_parent_setters() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());

        assert!(rapier_unity_collider_set_translation_wrt_parent(
            world_id,
            collider,
            RapierUnityVector3 {
                x: 1.0,
                y: 2.0,
                z: 3.0,
            },
        ));
        assert!(rapier_unity_collider_set_position_wrt_parent(
            world_id,
            collider,
            RapierUnityTransform {
                position_x: -1.0,
                position_y: 0.5,
                position_z: 0.0,
                ..RapierUnityTransform::default()
            },
        ));

        // Missing collider returns false.
        assert!(!rapier_unity_collider_set_translation_wrt_parent(
            world_id,
            RapierUnityColliderHandle::INVALID,
            RapierUnityVector3::default(),
        ));

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn trimesh_collider_supports_ground_plane() {
        let world_id = rapier_unity_world_create();
        let ground = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(ground.is_valid());

        // A flat quad on the XZ plane built from two triangles.
        let vertices: [f32; 12] = [
            -10.0, 0.0, -10.0, 10.0, 0.0, -10.0, 10.0, 0.0, 10.0, -10.0, 0.0, 10.0,
        ];
        let indices: [u32; 6] = [0, 1, 2, 0, 2, 3];
        let collider = unsafe {
            rapier_unity_collider_create_trimesh(
                world_id,
                ground,
                vertices.as_ptr(),
                4,
                indices.as_ptr(),
                6,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(collider.is_valid());

        // Odd index count is rejected.
        let bad = unsafe {
            rapier_unity_collider_create_trimesh(
                world_id,
                ground,
                vertices.as_ptr(),
                4,
                indices.as_ptr(),
                5,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(!bad.is_valid());

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn convex_hull_collider_from_point_cloud() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);

        // A unit cube's corners.
        let vertices: [f32; 24] = [
            -0.5, -0.5, -0.5, 0.5, -0.5, -0.5, 0.5, 0.5, -0.5, -0.5, 0.5, -0.5, -0.5, -0.5, 0.5,
            0.5, -0.5, 0.5, 0.5, 0.5, 0.5, -0.5, 0.5, 0.5,
        ];
        let collider = unsafe {
            rapier_unity_collider_create_convex_hull(
                world_id,
                body,
                vertices.as_ptr(),
                8,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(collider.is_valid());

        // Empty point cloud is rejected.
        let empty = unsafe {
            rapier_unity_collider_create_convex_hull(
                world_id,
                body,
                std::ptr::null(),
                0,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(!empty.is_valid());

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn heightfield_collider_requires_consistent_dimensions() {
        let world_id = rapier_unity_world_create();
        let ground = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );

        let heights: [f32; 9] = [0.0, 0.1, 0.0, 0.1, 0.2, 0.1, 0.0, 0.1, 0.0];
        let scale = RapierUnityVector3 {
            x: 10.0,
            y: 1.0,
            z: 10.0,
        };
        let collider = unsafe {
            rapier_unity_collider_create_heightfield(
                world_id,
                ground,
                heights.as_ptr(),
                3,
                3,
                scale,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(collider.is_valid());

        // Zero dimensions are rejected.
        let bad = unsafe {
            rapier_unity_collider_create_heightfield(
                world_id,
                ground,
                std::ptr::null(),
                0,
                0,
                scale,
                RapierUnityMeshColliderDesc::default(),
            )
        };
        assert!(!bad.is_valid());

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn filtered_raycast_can_exclude_a_collider() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());
        // Scene queries read the broad-phase BVH from the most recent step.
        assert!(rapier_unity_world_step(world_id));

        // Ray travelling down the -Y axis toward the box at the origin.
        let ray = RapierUnityRay {
            origin_x: 0.0,
            origin_y: 5.0,
            origin_z: 0.0,
            direction_x: 0.0,
            direction_y: -1.0,
            direction_z: 0.0,
        };

        let mut hit = RapierUnityRaycastHit::default();
        assert!(unsafe {
            rapier_unity_raycast_filtered(
                world_id,
                ray,
                10.0,
                true,
                RapierUnityQueryFilter::default(),
                &mut hit,
            )
        });
        assert_eq!(hit.collider.index, collider.index);

        // Excluding that collider yields no hit.
        let filter = RapierUnityQueryFilter {
            use_exclude_collider: 1,
            exclude_collider: collider,
            ..RapierUnityQueryFilter::default()
        };
        assert!(!unsafe {
            rapier_unity_raycast_filtered(world_id, ray, 10.0, true, filter, &mut hit)
        });

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn project_point_finds_nearest_surface() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        // Default box collider is a 1m cube centred at the origin (half extent 0.5).
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());
        assert!(rapier_unity_world_step(world_id));

        let mut projection = RapierUnityPointProjection::default();
        assert!(unsafe {
            rapier_unity_project_point(
                world_id,
                0.0,
                2.0,
                0.0,
                true,
                RapierUnityQueryFilter::default(),
                &mut projection,
            )
        });
        assert_eq!(projection.collider.index, collider.index);
        assert!((projection.point_y - 0.5).abs() < 1e-4);
        assert_eq!(projection.is_inside, 0);

        // A point inside the cube reports is_inside with solid projection.
        assert!(unsafe {
            rapier_unity_project_point(
                world_id,
                0.0,
                0.0,
                0.0,
                true,
                RapierUnityQueryFilter::default(),
                &mut projection,
            )
        });
        assert_eq!(projection.is_inside, 1);

        // A filter that excludes the only collider must return no projection
        // (and must not abort the process via parry's internal unwrap).
        let exclude_all = RapierUnityQueryFilter {
            use_exclude_collider: 1,
            exclude_collider: collider,
            ..RapierUnityQueryFilter::default()
        };
        assert!(!unsafe {
            rapier_unity_project_point(world_id, 0.0, 2.0, 0.0, true, exclude_all, &mut projection)
        });

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn intersection_with_point_reports_containing_collider() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());
        assert!(rapier_unity_world_step(world_id));

        let mut found = RapierUnityColliderHandle::INVALID;
        assert!(unsafe {
            rapier_unity_intersection_with_point(
                world_id,
                0.0,
                0.0,
                0.0,
                RapierUnityQueryFilter::default(),
                &mut found,
            )
        });
        assert_eq!(found.index, collider.index);

        // A point well outside the cube finds nothing.
        assert!(!unsafe {
            rapier_unity_intersection_with_point(
                world_id,
                10.0,
                10.0,
                10.0,
                RapierUnityQueryFilter::default(),
                &mut found,
            )
        });

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn raycast_all_collects_multiple_colliders() {
        let world_id = rapier_unity_world_create();

        // Two stacked static boxes along the ray path.
        for y in [0.0_f32, 3.0_f32] {
            let body = rapier_unity_body_create(
                world_id,
                RapierUnityRigidBodyDesc {
                    body_type: RapierUnityRigidBodyType::Fixed as u32,
                    position_y: y,
                    ..RapierUnityRigidBodyDesc::default()
                },
            );
            assert!(rapier_unity_collider_create_box(
                world_id,
                body,
                RapierUnityBoxColliderDesc::default()
            )
            .is_valid());
        }
        assert!(rapier_unity_world_step(world_id));

        let ray = RapierUnityRay {
            origin_x: 0.0,
            origin_y: 10.0,
            origin_z: 0.0,
            direction_x: 0.0,
            direction_y: -1.0,
            direction_z: 0.0,
        };
        let mut hits = [RapierUnityRaycastHit::default(); 8];
        let count = unsafe {
            rapier_unity_raycast_all(
                world_id,
                ray,
                20.0,
                true,
                RapierUnityQueryFilter::default(),
                hits.as_mut_ptr(),
                hits.len(),
            )
        };
        assert_eq!(count, 2);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn cast_shape_hits_collider_along_velocity() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(attach_test_box(world_id, body).is_valid());
        assert!(rapier_unity_world_step(world_id));

        // A ball starting above the box, cast downward, should hit it.
        let shape = RapierUnityQueryShape {
            shape_type: 0,
            half_extents_x: 0.0,
            half_extents_y: 0.0,
            half_extents_z: 0.0,
            radius: 0.5,
            half_height: 0.0,
        };
        let mut hit = RapierUnityShapeCastHit {
            collider: RapierUnityColliderHandle::INVALID,
            time_of_impact: -1.0,
            witness1_x: 0.0,
            witness1_y: 0.0,
            witness1_z: 0.0,
            witness2_x: 0.0,
            witness2_y: 0.0,
            witness2_z: 0.0,
            normal1_x: 0.0,
            normal1_y: 0.0,
            normal1_z: 0.0,
            normal2_x: 0.0,
            normal2_y: 0.0,
            normal2_z: 0.0,
            status: 0,
        };
        let pose = RapierUnityTransform {
            position_y: 5.0,
            ..RapierUnityTransform::default()
        };
        let velocity = RapierUnityVector3 {
            x: 0.0,
            y: -1.0,
            z: 0.0,
        };
        assert!(unsafe {
            rapier_unity_cast_shape(
                world_id,
                pose,
                velocity,
                shape,
                10.0,
                true,
                RapierUnityQueryFilter::default(),
                &mut hit,
            )
        });
        assert!(hit.time_of_impact > 0.0);

        // An unknown shape type is rejected.
        let bad_shape = RapierUnityQueryShape {
            shape_type: 99,
            ..shape
        };
        assert!(!unsafe {
            rapier_unity_cast_shape(
                world_id,
                pose,
                velocity,
                bad_shape,
                10.0,
                true,
                RapierUnityQueryFilter::default(),
                &mut hit,
            )
        });

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn intersect_shape_reports_overlapping_colliders() {
        let world_id = rapier_unity_world_create();
        let body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());
        assert!(rapier_unity_world_step(world_id));

        // A cuboid overlapping the origin box.
        let shape = RapierUnityQueryShape {
            shape_type: 1,
            half_extents_x: 0.5,
            half_extents_y: 0.5,
            half_extents_z: 0.5,
            radius: 0.0,
            half_height: 0.0,
        };
        let mut found = [RapierUnityColliderHandle::INVALID; 4];
        let count = unsafe {
            rapier_unity_intersect_shape(
                world_id,
                RapierUnityTransform::default(),
                shape,
                RapierUnityQueryFilter::default(),
                found.as_mut_ptr(),
                found.len(),
            )
        };
        assert_eq!(count, 1);
        assert_eq!(found[0].index, collider.index);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collider_event_config_roundtrip() {
        let world_id = rapier_unity_world_create();
        let body = create_test_body(world_id);
        let collider = attach_test_box(world_id, body);
        assert!(collider.is_valid());

        // COLLISION_EVENTS (1) | CONTACT_FORCE_EVENTS (2)
        assert!(rapier_unity_collider_set_active_events(
            world_id, collider, 3
        ));
        assert!(rapier_unity_collider_set_contact_force_event_threshold(
            world_id, collider, 2.5
        ));

        let mut flags = 0_u32;
        let mut threshold = 0.0_f32;
        assert!(unsafe { rapier_unity_collider_get_active_events(world_id, collider, &mut flags) });
        assert!(unsafe {
            rapier_unity_collider_get_contact_force_event_threshold(
                world_id,
                collider,
                &mut threshold,
            )
        });
        assert_eq!(flags, 3);
        assert_eq!(threshold, 2.5);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn collision_events_are_reported_for_active_colliders() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_gravity(world_id, 0.0, -9.81, 0.0));
        assert!(rapier_unity_world_set_timestep(world_id, 1.0 / 60.0));

        // Fixed ground box at the origin.
        let ground = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(rapier_unity_collider_create_box(
            world_id,
            ground,
            RapierUnityBoxColliderDesc::default()
        )
        .is_valid());

        // Dynamic box falling onto the ground, with collision events enabled.
        let faller = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_y: 1.5,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let faller_collider = rapier_unity_collider_create_box(
            world_id,
            faller,
            RapierUnityBoxColliderDesc::default(),
        );
        assert!(faller_collider.is_valid());
        assert!(rapier_unity_collider_set_active_events(
            world_id,
            faller_collider,
            1
        ));

        let mut started_events = 0;
        let mut buffer = [RapierUnityCollisionEvent {
            collider1: RapierUnityColliderHandle::INVALID,
            collider2: RapierUnityColliderHandle::INVALID,
            started: 0,
            flags: 0,
        }; 16];

        for _ in 0..120 {
            assert!(rapier_unity_world_step(world_id));
            let count = unsafe {
                rapier_unity_drain_collision_events(world_id, buffer.as_mut_ptr(), buffer.len())
            };
            for event in buffer.iter().take(count) {
                if event.started == 1 {
                    started_events += 1;
                }
            }
        }

        assert!(started_events >= 1);

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn fixed_joint_keeps_bodies_together() {
        let world_id = rapier_unity_world_create();
        assert!(rapier_unity_world_set_gravity(world_id, 0.0, -9.81, 0.0));
        assert!(rapier_unity_world_set_timestep(world_id, 1.0 / 60.0));

        let anchor_body = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                position_y: 5.0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let hanging = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_y: 5.0,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(attach_test_box(world_id, hanging).is_valid());

        let joint = rapier_unity_joint_create_fixed(
            world_id,
            anchor_body,
            hanging,
            RapierUnityVector3::default(),
            RapierUnityVector3::default(),
        );
        assert!(joint.is_valid());

        for _ in 0..120 {
            assert!(rapier_unity_world_step(world_id));
        }

        // Without the joint the dynamic body would fall far under gravity; the
        // fixed joint holds it near its initial height.
        let mut transform = RapierUnityTransform::default();
        assert!(unsafe { rapier_unity_body_get_transform(world_id, hanging, &mut transform) });
        assert!(transform.position_y > 4.0);

        assert!(rapier_unity_joint_remove(world_id, joint));
        assert!(!rapier_unity_joint_remove(world_id, joint));

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn revolute_joint_motor_and_limits_configurable() {
        let world_id = rapier_unity_world_create();
        let base = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Fixed as u32,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        let arm = rapier_unity_body_create(
            world_id,
            RapierUnityRigidBodyDesc {
                body_type: RapierUnityRigidBodyType::Dynamic as u32,
                position_x: 1.0,
                can_sleep: 0,
                ..RapierUnityRigidBodyDesc::default()
            },
        );
        assert!(attach_test_box(world_id, arm).is_valid());

        let joint = rapier_unity_joint_create_revolute(
            world_id,
            base,
            arm,
            RapierUnityVector3::default(),
            RapierUnityVector3 {
                x: -1.0,
                y: 0.0,
                z: 0.0,
            },
            RapierUnityVector3 {
                x: 0.0,
                y: 0.0,
                z: 1.0,
            },
        );
        assert!(joint.is_valid());

        // AngZ axis is index 5; configure limits and a velocity motor.
        assert!(rapier_unity_joint_set_limits(world_id, joint, 5, -1.0, 1.0));
        assert!(rapier_unity_joint_set_motor_velocity(
            world_id, joint, 5, 2.0, 0.5
        ));
        assert!(rapier_unity_joint_set_motor_max_force(
            world_id, joint, 5, 100.0
        ));
        assert!(rapier_unity_joint_set_motor_position(
            world_id, joint, 5, 0.5, 50.0, 5.0
        ));

        // An out-of-range axis index is rejected.
        assert!(!rapier_unity_joint_set_limits(
            world_id, joint, 9, -1.0, 1.0
        ));

        for _ in 0..30 {
            assert!(rapier_unity_world_step(world_id));
        }

        assert!(rapier_unity_world_destroy(world_id));
    }

    #[test]
    fn snapshot_roundtrip_restores_hash_and_future_steps() {
        let world_id = rapier_unity_world_create();
        create_stable_test_body(world_id, 10, 10.0);

        for _ in 0..30 {
            assert!(rapier_unity_world_step(world_id));
        }

        let source_hash = rapier_unity_world_state_hash(world_id);
        let size = rapier_unity_world_snapshot_size(world_id);
        assert!(size > 0);

        let mut bytes = vec![0_u8; size];
        assert!(unsafe {
            rapier_unity_world_snapshot_write(world_id, bytes.as_mut_ptr(), bytes.len())
        });

        let restored_world = rapier_unity_world_create();
        assert!(unsafe {
            rapier_unity_world_snapshot_read(restored_world, bytes.as_ptr(), bytes.len())
        });
        assert_eq!(source_hash, rapier_unity_world_state_hash(restored_world));

        for _ in 0..30 {
            assert!(rapier_unity_world_step(world_id));
            assert!(rapier_unity_world_step(restored_world));
        }

        assert_eq!(
            rapier_unity_world_state_hash(world_id),
            rapier_unity_world_state_hash(restored_world)
        );

        assert!(rapier_unity_world_destroy(world_id));
        assert!(rapier_unity_world_destroy(restored_world));
    }
}
