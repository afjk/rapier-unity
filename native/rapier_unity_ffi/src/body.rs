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

#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct RapierUnityVector3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

impl RapierUnityVector3 {
    fn from_vector(vector: &Vector<Real>) -> Self {
        Self {
            x: vector.x,
            y: vector.y,
            z: vector.z,
        }
    }
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityRigidBodyState {
    pub transform: RapierUnityTransform,
    pub linear_velocity_x: f32,
    pub linear_velocity_y: f32,
    pub linear_velocity_z: f32,
    pub angular_velocity_x: f32,
    pub angular_velocity_y: f32,
    pub angular_velocity_z: f32,
    pub sleeping: u8,
    pub enabled: u8,
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

    let handle = world.bodies.insert(builder.build());
    world.body_can_sleep.insert(handle, desc.can_sleep != 0);
    handle.into()
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
        world.body_can_sleep.remove(&handle);
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

    if world
        .body_stable_ids
        .iter()
        .any(|(other_handle, other_stable_id)| {
            *other_handle != handle && *other_stable_id == stable_id
        })
    {
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

pub fn get_body_state(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<RapierUnityRigidBodyState> {
    if !body.is_valid() {
        return None;
    }

    world.bodies.get(body.into()).map(|body| {
        let linvel = body.linvel();
        let angvel = body.angvel();
        RapierUnityRigidBodyState {
            transform: RapierUnityTransform::from_pose(body.position()),
            linear_velocity_x: linvel.x,
            linear_velocity_y: linvel.y,
            linear_velocity_z: linvel.z,
            angular_velocity_x: angvel.x,
            angular_velocity_y: angvel.y,
            angular_velocity_z: angvel.z,
            sleeping: u8::from(body.is_sleeping()),
            enabled: u8::from(body.is_enabled()),
        }
    })
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

/// Runs `f` against the body referenced by `body`, returning `true` when it exists.
fn with_body_mut(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    f: impl FnOnce(&mut RigidBody),
) -> bool {
    if !body.is_valid() {
        return false;
    }

    if let Some(body) = world.bodies.get_mut(body.into()) {
        f(body);
        true
    } else {
        false
    }
}

/// Reads a value from the body referenced by `body`, returning `None` when it is missing.
fn map_body<T>(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    f: impl FnOnce(&RigidBody) -> T,
) -> Option<T> {
    if !body.is_valid() {
        return None;
    }

    world.bodies.get(body.into()).map(f)
}

pub fn get_body_linvel(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<RapierUnityVector3> {
    map_body(world, body, |body| {
        RapierUnityVector3::from_vector(body.linvel())
    })
}

pub fn set_body_linvel(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    velocity: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_linvel(Vector::new(velocity.x, velocity.y, velocity.z), wake_up);
    })
}

pub fn get_body_angvel(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<RapierUnityVector3> {
    map_body(world, body, |body| {
        RapierUnityVector3::from_vector(body.angvel())
    })
}

pub fn set_body_angvel(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    velocity: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_angvel(Vector::new(velocity.x, velocity.y, velocity.z), wake_up);
    })
}

pub fn get_body_linear_damping(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<f32> {
    map_body(world, body, |body| body.linear_damping())
}

pub fn set_body_linear_damping(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    damping: f32,
) -> bool {
    with_body_mut(world, body, |body| body.set_linear_damping(damping))
}

pub fn get_body_angular_damping(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<f32> {
    map_body(world, body, |body| body.angular_damping())
}

pub fn set_body_angular_damping(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    damping: f32,
) -> bool {
    with_body_mut(world, body, |body| body.set_angular_damping(damping))
}

pub fn get_body_gravity_scale(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<f32> {
    map_body(world, body, |body| body.gravity_scale())
}

pub fn set_body_gravity_scale(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    scale: f32,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| body.set_gravity_scale(scale, wake_up))
}

pub fn get_body_ccd_enabled(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<bool> {
    map_body(world, body, |body| body.is_ccd_enabled())
}

pub fn set_body_ccd_enabled(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    enabled: bool,
) -> bool {
    with_body_mut(world, body, |body| body.enable_ccd(enabled))
}

pub fn get_body_soft_ccd_prediction(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<f32> {
    map_body(world, body, |body| body.soft_ccd_prediction())
}

pub fn set_body_soft_ccd_prediction(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    prediction: f32,
) -> bool {
    with_body_mut(world, body, |body| body.set_soft_ccd_prediction(prediction))
}

pub fn get_body_enabled(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<bool> {
    map_body(world, body, |body| body.is_enabled())
}

pub fn set_body_enabled(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    enabled: bool,
) -> bool {
    with_body_mut(world, body, |body| body.set_enabled(enabled))
}

pub fn add_body_force(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    force: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.add_force(Vector::new(force.x, force.y, force.z), wake_up);
    })
}

pub fn add_body_torque(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    torque: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.add_torque(Vector::new(torque.x, torque.y, torque.z), wake_up);
    })
}

pub fn apply_body_impulse(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    impulse: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.apply_impulse(Vector::new(impulse.x, impulse.y, impulse.z), wake_up);
    })
}

pub fn apply_body_torque_impulse(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    impulse: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.apply_torque_impulse(Vector::new(impulse.x, impulse.y, impulse.z), wake_up);
    })
}

pub fn set_body_next_kinematic_translation(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    translation: RapierUnityVector3,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_next_kinematic_translation(Vector::new(
            translation.x,
            translation.y,
            translation.z,
        ));
    })
}

pub fn set_body_next_kinematic_rotation(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    rotation: RapierUnityTransform,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_next_kinematic_rotation(rotation.to_pose().rotation);
    })
}

pub fn set_body_enabled_rotations(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    allow_x: bool,
    allow_y: bool,
    allow_z: bool,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_enabled_rotations(allow_x, allow_y, allow_z, wake_up);
    })
}

pub fn set_body_enabled_translations(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    allow_x: bool,
    allow_y: bool,
    allow_z: bool,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_enabled_translations(allow_x, allow_y, allow_z, wake_up);
    })
}

pub fn set_body_sleeping(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    sleeping: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        if sleeping {
            body.sleep();
        } else {
            body.wake_up(true);
        }
    })
}

pub fn add_body_force_at_point(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    force: RapierUnityVector3,
    point: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.add_force_at_point(
            Vector::new(force.x, force.y, force.z),
            Point::new(point.x, point.y, point.z),
            wake_up,
        );
    })
}

pub fn apply_body_impulse_at_point(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    impulse: RapierUnityVector3,
    point: RapierUnityVector3,
    wake_up: bool,
) -> bool {
    with_body_mut(world, body, |body| {
        body.apply_impulse_at_point(
            Vector::new(impulse.x, impulse.y, impulse.z),
            Point::new(point.x, point.y, point.z),
            wake_up,
        );
    })
}

pub fn get_body_additional_solver_iterations(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<u32> {
    map_body(world, body, |body| {
        body.additional_solver_iterations().min(u32::MAX as usize) as u32
    })
}

pub fn set_body_additional_solver_iterations(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    iterations: u32,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_additional_solver_iterations(iterations as usize);
    })
}

pub fn get_body_mass(world: &RapierUnityWorld, body: RapierUnityRigidBodyHandle) -> Option<f32> {
    map_body(world, body, |body| body.mass())
}

pub fn get_body_dominance_group(
    world: &RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
) -> Option<i32> {
    map_body(world, body, |body| i32::from(body.dominance_group()))
}

pub fn set_body_dominance_group(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    dominance: i32,
) -> bool {
    with_body_mut(world, body, |body| {
        body.set_dominance_group(dominance.clamp(i8::MIN as i32, i8::MAX as i32) as i8);
    })
}
