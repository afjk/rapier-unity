mod body;
mod collider;
mod handles;
mod hash;
mod query;
mod snapshot;
mod world;

pub use body::{RapierUnityRigidBodyDesc, RapierUnityRigidBodyType, RapierUnityTransform};
pub use collider::{
    RapierUnityBoxColliderDesc, RapierUnityCapsuleColliderDesc, RapierUnitySphereColliderDesc,
};
pub use handles::{RapierUnityColliderHandle, RapierUnityRigidBodyHandle};
pub use query::{RapierUnityRay, RapierUnityRaycastHit};

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
    fn snapshot_api_documents_current_stub_behavior() {
        let world_id = rapier_unity_world_create();
        let mut byte = 0_u8;

        assert_eq!(rapier_unity_world_snapshot_size(world_id), 0);
        assert!(unsafe { rapier_unity_world_snapshot_write(world_id, &mut byte, 0) });
        assert!(!unsafe { rapier_unity_world_snapshot_read(world_id, &byte, 1) });

        assert!(rapier_unity_world_destroy(world_id));
    }
}
