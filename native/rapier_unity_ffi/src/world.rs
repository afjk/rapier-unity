use std::collections::HashMap;
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::{LazyLock, Mutex};

use rapier3d::prelude::*;

pub struct RapierUnityWorld {
    pub gravity: Vector<Real>,
    pub integration_parameters: IntegrationParameters,
    pub physics_pipeline: PhysicsPipeline,
    pub islands: IslandManager,
    pub broad_phase: BroadPhaseBvh,
    pub narrow_phase: NarrowPhase,
    pub bodies: RigidBodySet,
    pub colliders: ColliderSet,
    pub impulse_joints: ImpulseJointSet,
    pub multibody_joints: MultibodyJointSet,
    pub ccd_solver: CCDSolver,
    pub body_stable_ids: HashMap<RigidBodyHandle, u64>,
    pub collider_stable_ids: HashMap<ColliderHandle, u64>,
    pub body_can_sleep: HashMap<RigidBodyHandle, bool>,
}

impl Default for RapierUnityWorld {
    fn default() -> Self {
        Self {
            gravity: Vector::new(0.0, -9.81, 0.0),
            integration_parameters: IntegrationParameters::default(),
            physics_pipeline: PhysicsPipeline::new(),
            islands: IslandManager::new(),
            broad_phase: BroadPhaseBvh::new(),
            narrow_phase: NarrowPhase::new(),
            bodies: RigidBodySet::new(),
            colliders: ColliderSet::new(),
            impulse_joints: ImpulseJointSet::new(),
            multibody_joints: MultibodyJointSet::new(),
            ccd_solver: CCDSolver::new(),
            body_stable_ids: HashMap::new(),
            collider_stable_ids: HashMap::new(),
            body_can_sleep: HashMap::new(),
        }
    }
}

impl RapierUnityWorld {
    pub fn step(&mut self) {
        self.physics_pipeline.step(
            &self.gravity,
            &self.integration_parameters,
            &mut self.islands,
            &mut self.broad_phase,
            &mut self.narrow_phase,
            &mut self.bodies,
            &mut self.colliders,
            &mut self.impulse_joints,
            &mut self.multibody_joints,
            &mut self.ccd_solver,
            &(),
            &(),
        );
    }

    pub fn set_gravity(&mut self, x: f32, y: f32, z: f32) {
        self.gravity = Vector::new(x, y, z);
    }

    pub fn set_timestep(&mut self, dt: f32) -> bool {
        if !dt.is_finite() || dt <= 0.0 {
            return false;
        }

        self.integration_parameters.dt = dt;
        self.integration_parameters.min_ccd_dt = dt / 100.0;
        true
    }
}

static NEXT_WORLD_ID: AtomicU64 = AtomicU64::new(1);
static WORLDS: LazyLock<Mutex<HashMap<u64, RapierUnityWorld>>> =
    LazyLock::new(|| Mutex::new(HashMap::new()));

pub fn create_world() -> u64 {
    let id = NEXT_WORLD_ID.fetch_add(1, Ordering::Relaxed);

    match WORLDS.lock() {
        Ok(mut worlds) => {
            worlds.insert(id, RapierUnityWorld::default());
            id
        }
        Err(_) => 0,
    }
}

pub fn destroy_world(world_id: u64) -> bool {
    if world_id == 0 {
        return false;
    }

    match WORLDS.lock() {
        Ok(mut worlds) => worlds.remove(&world_id).is_some(),
        Err(_) => false,
    }
}

pub fn with_world<T>(world_id: u64, f: impl FnOnce(&RapierUnityWorld) -> T) -> Option<T> {
    if world_id == 0 {
        return None;
    }

    let worlds = WORLDS.lock().ok()?;
    let world = worlds.get(&world_id)?;
    Some(f(world))
}

pub fn with_world_mut<T>(world_id: u64, f: impl FnOnce(&mut RapierUnityWorld) -> T) -> Option<T> {
    if world_id == 0 {
        return None;
    }

    let mut worlds = WORLDS.lock().ok()?;
    let world = worlds.get_mut(&world_id)?;
    Some(f(world))
}
