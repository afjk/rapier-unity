use std::sync::Mutex;

use rapier3d::pipeline::EventHandler;
use rapier3d::prelude::*;

use crate::handles::RapierUnityColliderHandle;
use crate::world::RapierUnityWorld;

/// A collision (intersection) event between two colliders, captured during a step.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityCollisionEvent {
    pub collider1: RapierUnityColliderHandle,
    pub collider2: RapierUnityColliderHandle,
    /// 1 when the colliders started touching, 0 when they stopped.
    pub started: u8,
    /// Raw `CollisionEventFlags` bits (e.g. SENSOR, REMOVED).
    pub flags: u32,
}

/// A contact-force event between two colliders, captured during a step.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityContactForceEvent {
    pub collider1: RapierUnityColliderHandle,
    pub collider2: RapierUnityColliderHandle,
    pub total_force_x: f32,
    pub total_force_y: f32,
    pub total_force_z: f32,
    pub total_force_magnitude: f32,
    pub max_force_direction_x: f32,
    pub max_force_direction_y: f32,
    pub max_force_direction_z: f32,
    pub max_force_magnitude: f32,
}

/// Collects physics events into in-memory buffers during a single step.
#[derive(Default)]
pub struct EventCollector {
    pub collisions: Mutex<Vec<RapierUnityCollisionEvent>>,
    pub contact_forces: Mutex<Vec<RapierUnityContactForceEvent>>,
}

impl EventHandler for EventCollector {
    fn handle_collision_event(
        &self,
        _bodies: &RigidBodySet,
        _colliders: &ColliderSet,
        event: CollisionEvent,
        _contact_pair: Option<&ContactPair>,
    ) {
        let collected = match event {
            CollisionEvent::Started(a, b, flags) => RapierUnityCollisionEvent {
                collider1: a.into(),
                collider2: b.into(),
                started: 1,
                flags: flags.bits(),
            },
            CollisionEvent::Stopped(a, b, flags) => RapierUnityCollisionEvent {
                collider1: a.into(),
                collider2: b.into(),
                started: 0,
                flags: flags.bits(),
            },
        };

        if let Ok(mut events) = self.collisions.lock() {
            events.push(collected);
        }
    }

    fn handle_contact_force_event(
        &self,
        dt: Real,
        _bodies: &RigidBodySet,
        _colliders: &ColliderSet,
        contact_pair: &ContactPair,
        total_force_magnitude: Real,
    ) {
        let event = ContactForceEvent::from_contact_pair(dt, contact_pair, total_force_magnitude);
        let collected = RapierUnityContactForceEvent {
            collider1: event.collider1.into(),
            collider2: event.collider2.into(),
            total_force_x: event.total_force.x,
            total_force_y: event.total_force.y,
            total_force_z: event.total_force.z,
            total_force_magnitude: event.total_force_magnitude,
            max_force_direction_x: event.max_force_direction.x,
            max_force_direction_y: event.max_force_direction.y,
            max_force_direction_z: event.max_force_direction.z,
            max_force_magnitude: event.max_force_magnitude,
        };

        if let Ok(mut events) = self.contact_forces.lock() {
            events.push(collected);
        }
    }
}

/// Copies up to `out.len()` collision events from the most recent step into
/// `out`, returning the number copied.
pub fn drain_collision_events(
    world: &RapierUnityWorld,
    out: &mut [RapierUnityCollisionEvent],
) -> usize {
    let count = out.len().min(world.collision_events.len());
    out[..count].copy_from_slice(&world.collision_events[..count]);
    count
}

/// Copies up to `out.len()` contact-force events from the most recent step into
/// `out`, returning the number copied.
pub fn drain_contact_force_events(
    world: &RapierUnityWorld,
    out: &mut [RapierUnityContactForceEvent],
) -> usize {
    let count = out.len().min(world.contact_force_events.len());
    out[..count].copy_from_slice(&world.contact_force_events[..count]);
    count
}
