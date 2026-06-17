use std::collections::{HashMap, HashSet};
use std::ptr;
use std::slice;

use rapier3d::control::PidController;
use rapier3d::prelude::*;
use serde::{Deserialize, Serialize};

use crate::hash::RAPIER_CORE_VERSION;
use crate::world::RapierUnityWorld;

const SNAPSHOT_FORMAT: &str = "rapier-unity-native-snapshot";
const SNAPSHOT_FORMAT_VERSION: u32 = 1;
const FFI_SCHEMA_VERSION: u32 = 3;

#[derive(Clone, Copy, Deserialize, Serialize)]
struct StableIdEntry {
    index: u32,
    generation: u32,
    stable_id: u64,
}

#[derive(Clone, Copy, Deserialize, Serialize)]
struct BodyCanSleepEntry {
    index: u32,
    generation: u32,
    can_sleep: bool,
}

#[derive(Clone, Copy, Deserialize, Serialize)]
struct PidControllerEntry {
    id: u64,
    axes: u8,
    lin_kp: [f32; 3],
    lin_kd: [f32; 3],
    ang_kp: [f32; 3],
    ang_kd: [f32; 3],
    lin_integral: [f32; 3],
    ang_integral: [f32; 3],
    lin_ki: [f32; 3],
    ang_ki: [f32; 3],
}

#[derive(Serialize)]
struct NativeSnapshotRef<'a> {
    format: &'static str,
    format_version: u32,
    ffi_schema_version: u32,
    rapier_core_version: &'static str,
    gravity: [f32; 3],
    integration_parameters: &'a IntegrationParameters,
    islands: &'a IslandManager,
    broad_phase: &'a BroadPhaseBvh,
    narrow_phase: &'a NarrowPhase,
    bodies: &'a RigidBodySet,
    colliders: &'a ColliderSet,
    impulse_joints: &'a ImpulseJointSet,
    multibody_joints: &'a MultibodyJointSet,
    ccd_solver: &'a CCDSolver,
    body_stable_ids: Vec<StableIdEntry>,
    collider_stable_ids: Vec<StableIdEntry>,
    body_can_sleep: Vec<BodyCanSleepEntry>,
    pid_controllers: Vec<PidControllerEntry>,
    next_pid_controller_id: u64,
}

#[derive(Deserialize)]
struct NativeSnapshot {
    format: String,
    format_version: u32,
    ffi_schema_version: u32,
    rapier_core_version: String,
    gravity: [f32; 3],
    integration_parameters: IntegrationParameters,
    islands: IslandManager,
    broad_phase: BroadPhaseBvh,
    narrow_phase: NarrowPhase,
    bodies: RigidBodySet,
    colliders: ColliderSet,
    impulse_joints: ImpulseJointSet,
    multibody_joints: MultibodyJointSet,
    ccd_solver: CCDSolver,
    body_stable_ids: Vec<StableIdEntry>,
    collider_stable_ids: Vec<StableIdEntry>,
    body_can_sleep: Vec<BodyCanSleepEntry>,
    pid_controllers: Vec<PidControllerEntry>,
    next_pid_controller_id: u64,
}

fn rigid_body_stable_entries(world: &RapierUnityWorld) -> Vec<StableIdEntry> {
    let mut entries: Vec<_> = world
        .body_stable_ids
        .iter()
        .filter_map(|(handle, stable_id)| {
            if *stable_id == 0 || world.bodies.get(*handle).is_none() {
                return None;
            }

            let (index, generation) = handle.into_raw_parts();
            Some(StableIdEntry {
                index,
                generation,
                stable_id: *stable_id,
            })
        })
        .collect();

    entries.sort_by_key(|entry| (entry.stable_id, entry.index, entry.generation));
    entries
}

fn collider_stable_entries(world: &RapierUnityWorld) -> Vec<StableIdEntry> {
    let mut entries: Vec<_> = world
        .collider_stable_ids
        .iter()
        .filter_map(|(handle, stable_id)| {
            if *stable_id == 0 || world.colliders.get(*handle).is_none() {
                return None;
            }

            let (index, generation) = handle.into_raw_parts();
            Some(StableIdEntry {
                index,
                generation,
                stable_id: *stable_id,
            })
        })
        .collect();

    entries.sort_by_key(|entry| (entry.stable_id, entry.index, entry.generation));
    entries
}

fn body_can_sleep_entries(world: &RapierUnityWorld) -> Vec<BodyCanSleepEntry> {
    let mut entries: Vec<_> = world
        .body_can_sleep
        .iter()
        .filter_map(|(handle, can_sleep)| {
            world.bodies.get(*handle)?;

            let (index, generation) = handle.into_raw_parts();
            Some(BodyCanSleepEntry {
                index,
                generation,
                can_sleep: *can_sleep,
            })
        })
        .collect();

    entries.sort_by_key(|entry| (entry.index, entry.generation));
    entries
}

fn vector_to_array(vector: &Vector<Real>) -> [f32; 3] {
    [vector.x, vector.y, vector.z]
}

fn vector_from_array(values: [f32; 3]) -> Vector<Real> {
    Vector::new(values[0], values[1], values[2])
}

fn pid_controller_entries(world: &RapierUnityWorld) -> Vec<PidControllerEntry> {
    let mut entries: Vec<_> = world
        .pid_controllers
        .iter()
        .filter_map(|(id, controller)| {
            if *id == 0 {
                return None;
            }

            Some(PidControllerEntry {
                id: *id,
                axes: controller.axes().bits(),
                lin_kp: vector_to_array(&controller.pd.lin_kp),
                lin_kd: vector_to_array(&controller.pd.lin_kd),
                ang_kp: vector_to_array(&controller.pd.ang_kp),
                ang_kd: vector_to_array(&controller.pd.ang_kd),
                lin_integral: vector_to_array(&controller.lin_integral),
                ang_integral: vector_to_array(&controller.ang_integral),
                lin_ki: vector_to_array(&controller.lin_ki),
                ang_ki: vector_to_array(&controller.ang_ki),
            })
        })
        .collect();

    entries.sort_by_key(|entry| entry.id);
    entries
}

fn serialize_snapshot(world: &RapierUnityWorld) -> Option<Vec<u8>> {
    let snapshot = NativeSnapshotRef {
        format: SNAPSHOT_FORMAT,
        format_version: SNAPSHOT_FORMAT_VERSION,
        ffi_schema_version: FFI_SCHEMA_VERSION,
        rapier_core_version: RAPIER_CORE_VERSION,
        gravity: [world.gravity.x, world.gravity.y, world.gravity.z],
        integration_parameters: &world.integration_parameters,
        islands: &world.islands,
        broad_phase: &world.broad_phase,
        narrow_phase: &world.narrow_phase,
        bodies: &world.bodies,
        colliders: &world.colliders,
        impulse_joints: &world.impulse_joints,
        multibody_joints: &world.multibody_joints,
        ccd_solver: &world.ccd_solver,
        body_stable_ids: rigid_body_stable_entries(world),
        collider_stable_ids: collider_stable_entries(world),
        body_can_sleep: body_can_sleep_entries(world),
        pid_controllers: pid_controller_entries(world),
        next_pid_controller_id: world.next_pid_controller_id,
    };

    bincode::serialize(&snapshot).ok()
}

fn validate_snapshot(snapshot: &NativeSnapshot) -> bool {
    snapshot.format == SNAPSHOT_FORMAT
        && snapshot.format_version == SNAPSHOT_FORMAT_VERSION
        && snapshot.ffi_schema_version == FFI_SCHEMA_VERSION
        && snapshot.rapier_core_version == RAPIER_CORE_VERSION
        && valid_body_stable_entries(snapshot)
        && valid_collider_stable_entries(snapshot)
        && valid_body_can_sleep_entries(snapshot)
        && valid_pid_controller_entries(snapshot)
}

fn valid_stable_entry(
    entry: &StableIdEntry,
    seen_handles: &mut HashSet<(u32, u32)>,
    seen_stable_ids: &mut HashSet<u64>,
) -> bool {
    entry.stable_id != 0
        && seen_handles.insert((entry.index, entry.generation))
        && seen_stable_ids.insert(entry.stable_id)
}

fn valid_body_stable_entries(snapshot: &NativeSnapshot) -> bool {
    let mut seen_handles = HashSet::new();
    let mut seen_stable_ids = HashSet::new();

    snapshot.body_stable_ids.iter().all(|entry| {
        let handle = RigidBodyHandle::from_raw_parts(entry.index, entry.generation);
        snapshot.bodies.get(handle).is_some()
            && valid_stable_entry(entry, &mut seen_handles, &mut seen_stable_ids)
    })
}

fn valid_collider_stable_entries(snapshot: &NativeSnapshot) -> bool {
    let mut seen_handles = HashSet::new();
    let mut seen_stable_ids = HashSet::new();

    snapshot.collider_stable_ids.iter().all(|entry| {
        let handle = ColliderHandle::from_raw_parts(entry.index, entry.generation);
        snapshot.colliders.get(handle).is_some()
            && valid_stable_entry(entry, &mut seen_handles, &mut seen_stable_ids)
    })
}

fn valid_body_can_sleep_entries(snapshot: &NativeSnapshot) -> bool {
    let mut seen_handles = HashSet::new();

    snapshot.body_can_sleep.iter().all(|entry| {
        let handle = RigidBodyHandle::from_raw_parts(entry.index, entry.generation);
        snapshot.bodies.get(handle).is_some()
            && seen_handles.insert((entry.index, entry.generation))
    })
}

fn valid_pid_controller_entries(snapshot: &NativeSnapshot) -> bool {
    let mut seen_ids = HashSet::new();
    snapshot.pid_controllers.iter().all(|entry| {
        entry.id != 0
            && entry.id < snapshot.next_pid_controller_id
            && AxesMask::from_bits(entry.axes).is_some()
            && seen_ids.insert(entry.id)
    })
}

fn restore_body_stable_ids(snapshot: &NativeSnapshot) -> HashMap<RigidBodyHandle, u64> {
    snapshot
        .body_stable_ids
        .iter()
        .filter_map(|entry| {
            if entry.stable_id == 0 {
                return None;
            }

            let handle = RigidBodyHandle::from_raw_parts(entry.index, entry.generation);
            snapshot
                .bodies
                .get(handle)
                .map(|_| (handle, entry.stable_id))
        })
        .collect()
}

fn restore_collider_stable_ids(snapshot: &NativeSnapshot) -> HashMap<ColliderHandle, u64> {
    snapshot
        .collider_stable_ids
        .iter()
        .filter_map(|entry| {
            if entry.stable_id == 0 {
                return None;
            }

            let handle = ColliderHandle::from_raw_parts(entry.index, entry.generation);
            snapshot
                .colliders
                .get(handle)
                .map(|_| (handle, entry.stable_id))
        })
        .collect()
}

fn restore_body_can_sleep(snapshot: &NativeSnapshot) -> HashMap<RigidBodyHandle, bool> {
    snapshot
        .body_can_sleep
        .iter()
        .filter_map(|entry| {
            let handle = RigidBodyHandle::from_raw_parts(entry.index, entry.generation);
            snapshot
                .bodies
                .get(handle)
                .map(|_| (handle, entry.can_sleep))
        })
        .collect()
}

fn restore_pid_controllers(snapshot: &NativeSnapshot) -> HashMap<u64, PidController> {
    snapshot
        .pid_controllers
        .iter()
        .filter_map(|entry| {
            let axes = AxesMask::from_bits(entry.axes)?;
            let mut controller = PidController::new(0.0, 0.0, 0.0, axes);
            controller.pd.lin_kp = vector_from_array(entry.lin_kp);
            controller.pd.lin_kd = vector_from_array(entry.lin_kd);
            controller.pd.ang_kp = vector_from_array(entry.ang_kp);
            controller.pd.ang_kd = vector_from_array(entry.ang_kd);
            controller.lin_integral = vector_from_array(entry.lin_integral);
            controller.ang_integral = vector_from_array(entry.ang_integral);
            controller.lin_ki = vector_from_array(entry.lin_ki);
            controller.ang_ki = vector_from_array(entry.ang_ki);
            Some((entry.id, controller))
        })
        .collect()
}

pub fn snapshot_size(world: &RapierUnityWorld) -> usize {
    serialize_snapshot(world).map_or(0, |bytes| bytes.len())
}

pub fn snapshot_write(world: &RapierUnityWorld, out_bytes: *mut u8, len: usize) -> bool {
    let Some(bytes) = serialize_snapshot(world) else {
        return false;
    };

    if bytes.len() != len {
        return false;
    }

    if len == 0 {
        return true;
    }

    if out_bytes.is_null() {
        return false;
    }

    unsafe {
        ptr::copy_nonoverlapping(bytes.as_ptr(), out_bytes, len);
    }
    true
}

pub fn snapshot_read(world: &mut RapierUnityWorld, bytes: *const u8, len: usize) -> bool {
    if len == 0 || bytes.is_null() {
        return false;
    }

    let bytes = unsafe { slice::from_raw_parts(bytes, len) };
    let Ok(snapshot) = bincode::deserialize::<NativeSnapshot>(bytes) else {
        return false;
    };

    if !validate_snapshot(&snapshot) {
        return false;
    }

    let body_stable_ids = restore_body_stable_ids(&snapshot);
    let collider_stable_ids = restore_collider_stable_ids(&snapshot);
    let body_can_sleep = restore_body_can_sleep(&snapshot);
    let pid_controllers = restore_pid_controllers(&snapshot);
    let next_pid_controller_id = world
        .next_pid_controller_id
        .max(snapshot.next_pid_controller_id)
        .max(
            pid_controllers
                .keys()
                .max()
                .copied()
                .unwrap_or(0)
                .saturating_add(1),
        );

    *world = RapierUnityWorld {
        gravity: Vector::new(
            snapshot.gravity[0],
            snapshot.gravity[1],
            snapshot.gravity[2],
        ),
        integration_parameters: snapshot.integration_parameters,
        physics_pipeline: PhysicsPipeline::new(),
        islands: snapshot.islands,
        broad_phase: snapshot.broad_phase,
        narrow_phase: snapshot.narrow_phase,
        bodies: snapshot.bodies,
        colliders: snapshot.colliders,
        impulse_joints: snapshot.impulse_joints,
        multibody_joints: snapshot.multibody_joints,
        ccd_solver: snapshot.ccd_solver,
        body_stable_ids,
        collider_stable_ids,
        body_can_sleep,
        collision_events: Vec::new(),
        contact_force_events: Vec::new(),
        pid_controllers,
        next_pid_controller_id,
    };

    true
}
