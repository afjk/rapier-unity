use rapier3d::dynamics::{
    FixedJointBuilder, JointAxis, PrismaticJointBuilder, RevoluteJointBuilder, RopeJointBuilder,
    SphericalJointBuilder, SpringJointBuilder,
};
use rapier3d::na::Unit;
use rapier3d::prelude::*;

use crate::body::RapierUnityVector3;
use crate::handles::{RapierUnityJointHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

fn point(value: RapierUnityVector3) -> Point<Real> {
    Point::new(value.x, value.y, value.z)
}

/// Normalizes an axis vector, falling back to the +Y axis for degenerate input.
fn axis_unit(value: RapierUnityVector3) -> UnitVector<Real> {
    Unit::try_new(Vector::new(value.x, value.y, value.z), 1.0e-6).unwrap_or_else(Vector::y_axis)
}

fn axis_from_u32(value: u32) -> Option<JointAxis> {
    match value {
        0 => Some(JointAxis::LinX),
        1 => Some(JointAxis::LinY),
        2 => Some(JointAxis::LinZ),
        3 => Some(JointAxis::AngX),
        4 => Some(JointAxis::AngY),
        5 => Some(JointAxis::AngZ),
        _ => None,
    }
}

fn insert_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    joint: impl Into<GenericJoint>,
) -> RapierUnityJointHandle {
    if !body1.is_valid() || !body2.is_valid() {
        return RapierUnityJointHandle::INVALID;
    }

    if world.bodies.get(body1.into()).is_none() || world.bodies.get(body2.into()).is_none() {
        return RapierUnityJointHandle::INVALID;
    }

    world
        .impulse_joints
        .insert(body1.into(), body2.into(), joint, true)
        .into()
}

pub fn create_fixed_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
) -> RapierUnityJointHandle {
    let joint = FixedJointBuilder::new()
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

pub fn create_spherical_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
) -> RapierUnityJointHandle {
    let joint = SphericalJointBuilder::new()
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

pub fn create_revolute_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    axis: RapierUnityVector3,
) -> RapierUnityJointHandle {
    let joint = RevoluteJointBuilder::new(axis_unit(axis))
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

pub fn create_prismatic_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    axis: RapierUnityVector3,
) -> RapierUnityJointHandle {
    let joint = PrismaticJointBuilder::new(axis_unit(axis))
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

pub fn create_rope_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    max_distance: f32,
) -> RapierUnityJointHandle {
    let joint = RopeJointBuilder::new(max_distance.max(0.0))
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

#[allow(clippy::too_many_arguments)]
pub fn create_spring_joint(
    world: &mut RapierUnityWorld,
    body1: RapierUnityRigidBodyHandle,
    body2: RapierUnityRigidBodyHandle,
    anchor1: RapierUnityVector3,
    anchor2: RapierUnityVector3,
    rest_length: f32,
    stiffness: f32,
    damping: f32,
) -> RapierUnityJointHandle {
    let joint = SpringJointBuilder::new(rest_length.max(0.0), stiffness.max(0.0), damping.max(0.0))
        .local_anchor1(point(anchor1))
        .local_anchor2(point(anchor2));
    insert_joint(world, body1, body2, joint)
}

pub fn remove_joint(world: &mut RapierUnityWorld, joint: RapierUnityJointHandle) -> bool {
    if !joint.is_valid() {
        return false;
    }

    world.impulse_joints.remove(joint.into(), true).is_some()
}

/// Runs `f` against the generic-joint data behind `joint`, returning `true` when it exists.
fn with_joint_mut(
    world: &mut RapierUnityWorld,
    joint: RapierUnityJointHandle,
    f: impl FnOnce(&mut GenericJoint),
) -> bool {
    if !joint.is_valid() {
        return false;
    }

    if let Some(joint) = world.impulse_joints.get_mut(joint.into(), true) {
        f(&mut joint.data);
        true
    } else {
        false
    }
}

pub fn set_joint_limits(
    world: &mut RapierUnityWorld,
    joint: RapierUnityJointHandle,
    axis: u32,
    min: f32,
    max: f32,
) -> bool {
    let Some(axis) = axis_from_u32(axis) else {
        return false;
    };

    with_joint_mut(world, joint, |joint| {
        joint.set_limits(axis, [min, max]);
    })
}

pub fn set_joint_motor_position(
    world: &mut RapierUnityWorld,
    joint: RapierUnityJointHandle,
    axis: u32,
    target_position: f32,
    stiffness: f32,
    damping: f32,
) -> bool {
    let Some(axis) = axis_from_u32(axis) else {
        return false;
    };

    with_joint_mut(world, joint, |joint| {
        joint.set_motor_position(axis, target_position, stiffness, damping);
    })
}

pub fn set_joint_motor_velocity(
    world: &mut RapierUnityWorld,
    joint: RapierUnityJointHandle,
    axis: u32,
    target_velocity: f32,
    factor: f32,
) -> bool {
    let Some(axis) = axis_from_u32(axis) else {
        return false;
    };

    with_joint_mut(world, joint, |joint| {
        joint.set_motor_velocity(axis, target_velocity, factor);
    })
}

pub fn set_joint_motor_max_force(
    world: &mut RapierUnityWorld,
    joint: RapierUnityJointHandle,
    axis: u32,
    max_force: f32,
) -> bool {
    let Some(axis) = axis_from_u32(axis) else {
        return false;
    };

    with_joint_mut(world, joint, |joint| {
        joint.set_motor_max_force(axis, max_force);
    })
}
