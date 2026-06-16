use std::collections::{HashMap, HashSet};
use std::ptr;
use std::slice;

use rapier3d::prelude::*;
use serde::{Deserialize, Serialize};

use crate::hash::RAPIER_CORE_VERSION;
use crate::world::RapierUnityWorld;

const SNAPSHOT_FORMAT: &str = "rapier-unity-native-snapshot";
const SNAPSHOT_FORMAT_VERSION: u32 = 1;
const FFI_SCHEMA_VERSION: u32 = 1;

#[derive(Clone, Copy, Deserialize, Serialize)]
struct StableIdEntry {
    index: u32,
    generation: u32,
    stable_id: u64,
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
    };

    true
}
