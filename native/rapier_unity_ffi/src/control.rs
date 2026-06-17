use rapier3d::control::PidController;
use rapier3d::dynamics::AxesMask;
use rapier3d::math::{Point, Vector};

use crate::body::{RapierUnityTransform, RapierUnityVector3};
use crate::handles::{RapierUnityPidControllerHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

fn axes_from_bits(axes: u8) -> AxesMask {
    AxesMask::from_bits(axes).unwrap_or_else(AxesMask::all)
}

pub fn create_pid_controller(
    world: &mut RapierUnityWorld,
    kp: f32,
    ki: f32,
    kd: f32,
    axes: u8,
) -> RapierUnityPidControllerHandle {
    let id = world.next_pid_controller_id;
    if id == 0 || id == u64::MAX {
        return RapierUnityPidControllerHandle::INVALID;
    }

    world.next_pid_controller_id = id + 1;
    world
        .pid_controllers
        .insert(id, PidController::new(kp, ki, kd, axes_from_bits(axes)));
    RapierUnityPidControllerHandle { id }
}

pub fn destroy_pid_controller(
    world: &mut RapierUnityWorld,
    controller: RapierUnityPidControllerHandle,
) -> bool {
    controller.is_valid() && world.pid_controllers.remove(&controller.id).is_some()
}

pub fn set_pid_controller_axes(
    world: &mut RapierUnityWorld,
    controller: RapierUnityPidControllerHandle,
    axes: u8,
) -> bool {
    let Some(axes) = AxesMask::from_bits(axes) else {
        return false;
    };

    if let Some(controller) = world.pid_controllers.get_mut(&controller.id) {
        controller.set_axes(axes);
        true
    } else {
        false
    }
}

pub fn reset_pid_controller_integrals(
    world: &mut RapierUnityWorld,
    controller: RapierUnityPidControllerHandle,
) -> bool {
    if let Some(controller) = world.pid_controllers.get_mut(&controller.id) {
        controller.reset_integrals();
        true
    } else {
        false
    }
}

pub fn apply_pid_linear_correction(
    world: &mut RapierUnityWorld,
    controller: RapierUnityPidControllerHandle,
    body: RapierUnityRigidBodyHandle,
    target_position: RapierUnityVector3,
    target_linear_velocity: RapierUnityVector3,
) -> bool {
    if !controller.is_valid() || !body.is_valid() {
        return false;
    }

    let Some(controller) = world.pid_controllers.get_mut(&controller.id) else {
        return false;
    };

    let Some(body) = world.bodies.get_mut(body.into()) else {
        return false;
    };

    let correction = controller.linear_rigid_body_correction(
        world.integration_parameters.dt,
        body,
        Point::new(target_position.x, target_position.y, target_position.z),
        Vector::new(
            target_linear_velocity.x,
            target_linear_velocity.y,
            target_linear_velocity.z,
        ),
    );
    body.set_linvel(*body.linvel() + correction, true);
    true
}

pub fn apply_pid_angular_correction(
    world: &mut RapierUnityWorld,
    controller: RapierUnityPidControllerHandle,
    body: RapierUnityRigidBodyHandle,
    target_rotation: RapierUnityTransform,
    target_angular_velocity: RapierUnityVector3,
) -> bool {
    if !controller.is_valid() || !body.is_valid() {
        return false;
    }

    let Some(controller) = world.pid_controllers.get_mut(&controller.id) else {
        return false;
    };

    let Some(body) = world.bodies.get_mut(body.into()) else {
        return false;
    };

    let correction = controller.angular_rigid_body_correction(
        world.integration_parameters.dt,
        body,
        target_rotation.to_pose().rotation,
        Vector::new(
            target_angular_velocity.x,
            target_angular_velocity.y,
            target_angular_velocity.z,
        ),
    );
    body.set_angvel(*body.angvel() + correction, true);
    true
}
