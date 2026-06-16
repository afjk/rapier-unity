use rapier3d::prelude::*;

use crate::body::RapierUnityTransform;
use crate::handles::{RapierUnityColliderHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityBoxColliderDesc {
    pub half_extents_x: f32,
    pub half_extents_y: f32,
    pub half_extents_z: f32,
    pub density: f32,
    pub is_sensor: u8,
    pub local_position_x: f32,
    pub local_position_y: f32,
    pub local_position_z: f32,
    pub local_rotation_x: f32,
    pub local_rotation_y: f32,
    pub local_rotation_z: f32,
    pub local_rotation_w: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnitySphereColliderDesc {
    pub radius: f32,
    pub density: f32,
    pub is_sensor: u8,
    pub local_position_x: f32,
    pub local_position_y: f32,
    pub local_position_z: f32,
    pub local_rotation_x: f32,
    pub local_rotation_y: f32,
    pub local_rotation_z: f32,
    pub local_rotation_w: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityCapsuleColliderDesc {
    pub half_height: f32,
    pub radius: f32,
    pub density: f32,
    pub is_sensor: u8,
    pub local_position_x: f32,
    pub local_position_y: f32,
    pub local_position_z: f32,
    pub local_rotation_x: f32,
    pub local_rotation_y: f32,
    pub local_rotation_z: f32,
    pub local_rotation_w: f32,
}

impl Default for RapierUnityBoxColliderDesc {
    fn default() -> Self {
        Self {
            half_extents_x: 0.5,
            half_extents_y: 0.5,
            half_extents_z: 0.5,
            density: 1.0,
            is_sensor: 0,
            local_position_x: 0.0,
            local_position_y: 0.0,
            local_position_z: 0.0,
            local_rotation_x: 0.0,
            local_rotation_y: 0.0,
            local_rotation_z: 0.0,
            local_rotation_w: 1.0,
        }
    }
}

impl Default for RapierUnitySphereColliderDesc {
    fn default() -> Self {
        Self {
            radius: 0.5,
            density: 1.0,
            is_sensor: 0,
            local_position_x: 0.0,
            local_position_y: 0.0,
            local_position_z: 0.0,
            local_rotation_x: 0.0,
            local_rotation_y: 0.0,
            local_rotation_z: 0.0,
            local_rotation_w: 1.0,
        }
    }
}

impl Default for RapierUnityCapsuleColliderDesc {
    fn default() -> Self {
        Self {
            half_height: 0.5,
            radius: 0.25,
            density: 1.0,
            is_sensor: 0,
            local_position_x: 0.0,
            local_position_y: 0.0,
            local_position_z: 0.0,
            local_rotation_x: 0.0,
            local_rotation_y: 0.0,
            local_rotation_z: 0.0,
            local_rotation_w: 1.0,
        }
    }
}

fn local_transform(
    position_x: f32,
    position_y: f32,
    position_z: f32,
    rotation_x: f32,
    rotation_y: f32,
    rotation_z: f32,
    rotation_w: f32,
) -> RapierUnityTransform {
    RapierUnityTransform {
        position_x,
        position_y,
        position_z,
        rotation_x,
        rotation_y,
        rotation_z,
        rotation_w,
    }
}

fn attach_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    collider: ColliderBuilder,
) -> RapierUnityColliderHandle {
    if !body.is_valid() || world.bodies.get(body.into()).is_none() {
        return RapierUnityColliderHandle::INVALID;
    }

    world
        .colliders
        .insert_with_parent(collider, body.into(), &mut world.bodies)
        .into()
}

pub fn create_box_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnityBoxColliderDesc,
) -> RapierUnityColliderHandle {
    let collider = ColliderBuilder::cuboid(
        desc.half_extents_x.max(0.0),
        desc.half_extents_y.max(0.0),
        desc.half_extents_z.max(0.0),
    )
    .density(desc.density.max(0.0))
    .sensor(desc.is_sensor != 0)
    .position(
        local_transform(
            desc.local_position_x,
            desc.local_position_y,
            desc.local_position_z,
            desc.local_rotation_x,
            desc.local_rotation_y,
            desc.local_rotation_z,
            desc.local_rotation_w,
        )
        .to_pose(),
    );

    attach_collider(world, body, collider)
}

pub fn create_sphere_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnitySphereColliderDesc,
) -> RapierUnityColliderHandle {
    let collider = ColliderBuilder::ball(desc.radius.max(0.0))
        .density(desc.density.max(0.0))
        .sensor(desc.is_sensor != 0)
        .position(
            local_transform(
                desc.local_position_x,
                desc.local_position_y,
                desc.local_position_z,
                desc.local_rotation_x,
                desc.local_rotation_y,
                desc.local_rotation_z,
                desc.local_rotation_w,
            )
            .to_pose(),
        );

    attach_collider(world, body, collider)
}

pub fn create_capsule_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    desc: RapierUnityCapsuleColliderDesc,
) -> RapierUnityColliderHandle {
    let collider = ColliderBuilder::capsule_y(desc.half_height.max(0.0), desc.radius.max(0.0))
        .density(desc.density.max(0.0))
        .sensor(desc.is_sensor != 0)
        .position(
            local_transform(
                desc.local_position_x,
                desc.local_position_y,
                desc.local_position_z,
                desc.local_rotation_x,
                desc.local_rotation_y,
                desc.local_rotation_z,
                desc.local_rotation_w,
            )
            .to_pose(),
        );

    attach_collider(world, body, collider)
}

pub fn destroy_collider(world: &mut RapierUnityWorld, collider: RapierUnityColliderHandle) -> bool {
    if !collider.is_valid() {
        return false;
    }

    let handle = collider.into();
    let removed = world
        .colliders
        .remove(handle, &mut world.islands, &mut world.bodies, true)
        .is_some();

    if removed {
        world.collider_stable_ids.remove(&handle);
    }

    removed
}

pub fn set_collider_stable_id(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    stable_id: u64,
) -> bool {
    if !collider.is_valid() || stable_id == 0 {
        return false;
    }

    let handle = collider.into();
    if world.colliders.get(handle).is_none() {
        return false;
    }

    if world
        .collider_stable_ids
        .iter()
        .any(|(other_handle, other_stable_id)| {
            *other_handle != handle && *other_stable_id == stable_id
        })
    {
        return false;
    }

    world.collider_stable_ids.insert(handle, stable_id);
    true
}
