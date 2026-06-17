use rapier3d::control::{CharacterAutostep, CharacterLength, KinematicCharacterController};
use rapier3d::na::Unit;
use rapier3d::prelude::*;

use crate::body::{RapierUnityTransform, RapierUnityVector3};
use crate::query::{RapierUnityQueryFilter, RapierUnityQueryShape};
use crate::world::RapierUnityWorld;

/// Configuration for a kinematic character controller move. Lengths are absolute
/// (world units). Angles are in radians.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityCharacterControllerDesc {
    pub up_x: f32,
    pub up_y: f32,
    pub up_z: f32,
    pub offset: f32,
    pub slide: u8,
    pub autostep_enabled: u8,
    pub autostep_max_height: f32,
    pub autostep_min_width: f32,
    pub autostep_include_dynamic: u8,
    pub max_slope_climb_angle: f32,
    pub min_slope_slide_angle: f32,
    pub snap_to_ground_enabled: u8,
    pub snap_to_ground_distance: f32,
    pub normal_nudge_factor: f32,
}

impl RapierUnityCharacterControllerDesc {
    fn to_controller(self) -> KinematicCharacterController {
        let mut controller = KinematicCharacterController::default();

        if let Some(up) = Unit::try_new(Vector::new(self.up_x, self.up_y, self.up_z), 1.0e-6) {
            controller.up = up;
        }

        controller.offset = CharacterLength::Absolute(self.offset.max(0.0));
        controller.slide = self.slide != 0;
        controller.autostep = if self.autostep_enabled != 0 {
            Some(CharacterAutostep {
                max_height: CharacterLength::Absolute(self.autostep_max_height.max(0.0)),
                min_width: CharacterLength::Absolute(self.autostep_min_width.max(0.0)),
                include_dynamic_bodies: self.autostep_include_dynamic != 0,
            })
        } else {
            None
        };
        controller.max_slope_climb_angle = self.max_slope_climb_angle;
        controller.min_slope_slide_angle = self.min_slope_slide_angle;
        controller.snap_to_ground = if self.snap_to_ground_enabled != 0 {
            Some(CharacterLength::Absolute(
                self.snap_to_ground_distance.max(0.0),
            ))
        } else {
            None
        };
        controller.normal_nudge_factor = self.normal_nudge_factor;

        controller
    }
}

/// The computed movement of a character controller move.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct RapierUnityCharacterMovement {
    pub translation_x: f32,
    pub translation_y: f32,
    pub translation_z: f32,
    pub grounded: u8,
    pub is_sliding_down_slope: u8,
}

pub fn move_character(
    world: &RapierUnityWorld,
    shape: RapierUnityQueryShape,
    position: RapierUnityTransform,
    desired_translation: RapierUnityVector3,
    dt: f32,
    desc: RapierUnityCharacterControllerDesc,
    filter: RapierUnityQueryFilter,
) -> Option<RapierUnityCharacterMovement> {
    let shared_shape = shape.to_shared_shape()?;
    let controller = desc.to_controller();

    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    let movement = controller.move_shape(
        dt,
        &query_pipeline,
        shared_shape.as_ref(),
        &position.to_pose(),
        Vector::new(
            desired_translation.x,
            desired_translation.y,
            desired_translation.z,
        ),
        |_collision| {},
    );

    Some(RapierUnityCharacterMovement {
        translation_x: movement.translation.x,
        translation_y: movement.translation.y,
        translation_z: movement.translation.z,
        grounded: u8::from(movement.grounded),
        is_sliding_down_slope: u8::from(movement.is_sliding_down_slope),
    })
}
