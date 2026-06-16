use rapier3d::math::{Isometry, Rotation, Vector};
use rapier3d::na::{Quaternion, Translation3};
use rapier3d::prelude::*;

use crate::handles::RapierUnityRigidBodyHandle;
use crate::world::RapierUnityWorld;

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityTransform {
    pub position_x: f32,
    pub position_y: f32,
    pub position_z: f32,
    pub rotation_x: f32,
    pub rotation_y: f32,
    pub rotation_z: f32,
    pub rotation_w: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub enum RapierUnityRigidBodyType {
    Dynamic = 0,
    Fixed = 1,
    KinematicPositionBased = 2,
    KinematicVelocityBased = 3,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityRigidBodyDesc {
    pub body_type: u32,
    pub position_x: f32,
    pub position_y: f32,
    pub position_z: f32,
    pub rotation_x: f32,
    pub rotation_y: f32,
    pub rotation_z: f32,
    pub rotation_w: f32,
    pub linear_velocity_x: f32,
    pub linear_velocity_y: f32,
    pub linear_velocity_z: f32,
    pub angular_velocity_x: f32,
    pub angular_velocity_y: f32,
    pub angular_velocity_z: f32,
    pub linear_damping: f32,
    pub angular_damping: f32,
    pub can_sleep: u8,
    pub ccd_enabled: u8,
}

impl Default for RapierUnityTransform {
    fn default() -> Self {
        Self {
            position_x: 0.0,
            position_y: 0.0,
            position_z: 0.0,
            rotation_x: 0.0,
            rotation_y: 0.0,
            rotation_z: 0.0,
            rotation_w: 1.0,
        }
    }
}

impl Default for RapierUnityRigidBodyDesc {
    fn default() -> Self {
        Self {
            body_type: RapierUnityRigidBodyType::Dynamic as u32,
            position_x: 0.0,
            position_y: 0.0,
            position_z: 0.0,
            rotation_x: 0.0,
            rotation_y: 0.0,
            rotation_z: 0.0,
            rotation_w: 1.0,
            linear_velocity_x: 0.0,
            linear_velocity_y: 0.0,
            linear_velocity_z: 0.0,
            angular_velocity_x: 0.0,
            angular_velocity_y: 0.0,
            angular_velocity_z: 0.0,
            linear_damping: 0.0,
            angular_damping: 0.0,
            can_sleep: 1,
            ccd_enabled: 0,
        }
    }
}

impl RapierUnityRigidBodyType {
    fn from_u32(value: u32) -> Option<Self> {
        match value {
            0 => Some(Self::Dynamic),
            1 => Some(Self::Fixed),
            2 => Some(Self::KinematicPositionBased),
            3 => Some(Self::KinematicVelocityBased),
            _ => None,
        }
    }

    fn to_rapier(self) -> RigidBodyType {
        match self {
            Self::Dynamic => RigidBodyType::Dynamic,
            Self::Fixed => RigidBodyType::Fixed,
            Self::KinematicPositionBased => RigidBodyType::KinematicPositionBased,
            Self::KinematicVelocityBased => RigidBodyType::KinematicVelocityBased,
        }
    }
}

impl RapierUnityTransform {
    pub fn to_pose(self) -> Isometry<Real> {
        let quaternion = Quaternion::new(
            self.rotation_w,
            self.rotation_x,
            self.rotation_y,
            self.rotation_z,
        );
        let rotation = if quaternion
            .coords
            .iter()
            .all(|component| component.is_finite())
            && quaternion.norm_squared() > f32::EPSILON
        {
            Rotation::new_normalize(quaternion)
        } else {
            Rotation::identity()
        };

        Isometry::from_parts(
            Translation3::from(Vector::new(
                self.position_x,
                self.position_y,
                self.position_z,
            )),
            rotation,
        )
    }

    pub fn from_pose(pose: &Isometry<Real>) -> Self {
        let rotation = pose.rotation;
        let quaternion = rotation.quaternion();
        let translation = pose.translation.vector;

        Self {
            position_x: translation.x,
            position_y: translation.y,
            position_z: translation.z,
            rotation_x: quaternion.i,
            rotation_y: quaternion.j,
            rotation_z: quaternion.k,
            rotation_w: quaternion.w,
        }
    }
}

impl RapierUnityRigidBodyDesc {
    fn transform(self) -> RapierUnityTransform {
        RapierUnityTransform {
            position_x: self.position_x,
            position_y: self.position_y,
            position_z: self.position_z,
            rotation_x: self.rotation_x,
            rotation_y: self.rotation_y,
            rotation_z: self.rotation_z,
            rotation_w: self.rotation_w,
        }
    }
}

pub fn create_body(
    world: &mut RapierUnityWorld,
    desc: RapierUnityRigidBodyDesc,
) -> RapierUnityRigidBodyHandle {
    let Some(body_type) = RapierUnityRigidBodyType::from_u32(desc.body_type) else {
        return RapierUnityRigidBodyHandle::INVALID;
    };

    let builder = match body_type.to_rapier() {
        RigidBodyType::Dynamic => RigidBodyBuilder::dynamic(),
        RigidBodyType::Fixed => RigidBodyBuilder::fixed(),
        RigidBodyType::KinematicPositionBased => RigidBodyBuilder::kinematic_position_based(),
        RigidBodyType::KinematicVelocityBased => RigidBodyBuilder::kinematic_velocity_based(),
    }
    .pose(desc.transform().to_pose())
    .linvel(Vector::new(
        desc.linear_velocity_x,
        desc.linear_velocity_y,
        desc.linear_velocity_z,
    ))
    .angvel(Vector::new(
        desc.angular_velocity_x,
        desc.angular_velocity_y,
        desc.angular_velocity_z,
    ))
    .linear_damping(desc.linear_damping)
    .angular_damping(desc.angular_damping)
    .can_sleep(desc.can_sleep != 0)
    .ccd_enabled(desc.ccd_enabled != 0);

    world.bodies.insert(builder.build()).into()
}

pub fn destroy_body(world: &mut RapierUnityWorld, body: RapierUnityRigidBodyHandle) -> bool {
    if !body.is_valid() {
        return false;
    }

    let handle = body.into();
    let removed = world
        .bodies
        .remove(
            handle,
            &mut world.islands,
            &mut world.colliders,
            &mut world.impulse_joints,
            &mut world.multibody_joints,
            true,
        )
        .is_some();

    if removed {
        world.body_stable_ids.remove(&handle);
        world
            .collider_stable_ids
            .retain(|handle, _| world.colliders.get(*handle).is_some());
    }

    removed
}

pub fn set_body_stable_id(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    stable_id: u64,
) -> bool {
    if !body.is_valid() || stable_id == 0 {
        return false;
    }

    let handle = body.into();
    if world.bodies.get(handle).is_none() {
        return false;
    }

    world.body_stable_ids.insert(handle, stable_id);
    true
}

pub fn get_body_transform(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<RapierUnityTransform> {
    if !body.is_valid() {
        return None;
    }

    world
        .bodies
        .get(body.into())
        .map(|body| RapierUnityTransform::from_pose(body.position()))
}

pub fn set_body_transform(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    transform: RapierUnityTransform,
) -> bool {
    if !body.is_valid() {
        return false;
    }

    if let Some(body) = world.bodies.get_mut(body.into()) {
        body.set_position(transform.to_pose(), true);
        true
    } else {
        false
    }
}
