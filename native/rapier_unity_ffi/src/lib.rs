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
    fn stable_id_hash_is_available_for_scene_sync_object_ids() {
        let id = b"scene-sync-object-1";
        assert_eq!(
            unsafe { rapier_unity_stable_id_hash(id.as_ptr(), id.len()) },
            hash::stable_id_hash_bytes(id)
        );
        assert_eq!(unsafe { rapier_unity_stable_id_hash(id.as_ptr(), 0) }, 0);
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
