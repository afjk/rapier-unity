use rapier3d::na::DMatrix;
use rapier3d::prelude::*;

use crate::body::{RapierUnityTransform, RapierUnityVector3};
use crate::handles::{RapierUnityColliderHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

/// Shared material and local-pose parameters for the mesh-based collider shapes
/// (trimesh, convex hull, heightfield). Vertex/index/height buffers are passed
/// separately because their length is variable.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityMeshColliderDesc {
    pub density: f32,
    pub friction: f32,
    pub restitution: f32,
    pub is_sensor: u8,
    pub local_position_x: f32,
    pub local_position_y: f32,
    pub local_position_z: f32,
    pub local_rotation_x: f32,
    pub local_rotation_y: f32,
    pub local_rotation_z: f32,
    pub local_rotation_w: f32,
}

impl Default for RapierUnityMeshColliderDesc {
    fn default() -> Self {
        Self {
            density: 1.0,
            friction: 0.5,
            restitution: 0.0,
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

impl RapierUnityMeshColliderDesc {
    fn apply(self, builder: ColliderBuilder) -> ColliderBuilder {
        builder
            .density(self.density.max(0.0))
            .friction(self.friction.max(0.0))
            .restitution(self.restitution.max(0.0))
            .sensor(self.is_sensor != 0)
            .position(
                local_transform(
                    self.local_position_x,
                    self.local_position_y,
                    self.local_position_z,
                    self.local_rotation_x,
                    self.local_rotation_y,
                    self.local_rotation_z,
                    self.local_rotation_w,
                )
                .to_pose(),
            )
    }
}

/// Converts a flat `[x, y, z, ...]` buffer into Rapier vertices.
fn slice_to_points(vertices: &[f32]) -> Vec<Point<Real>> {
    vertices
        .chunks_exact(3)
        .map(|chunk| Point::new(chunk[0], chunk[1], chunk[2]))
        .collect()
}

/// Converts a flat index buffer into triangle index triples, returning `None`
/// when the buffer length is not a multiple of three.
fn slice_to_triangles(indices: &[u32]) -> Option<Vec<[u32; 3]>> {
    if indices.len() % 3 != 0 {
        return None;
    }

    Some(
        indices
            .chunks_exact(3)
            .map(|chunk| [chunk[0], chunk[1], chunk[2]])
            .collect(),
    )
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityBoxColliderDesc {
    pub half_extents_x: f32,
    pub half_extents_y: f32,
    pub half_extents_z: f32,
    pub density: f32,
    pub friction: f32,
    pub restitution: f32,
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
    pub friction: f32,
    pub restitution: f32,
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
    pub friction: f32,
    pub restitution: f32,
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
            friction: 0.5,
            restitution: 0.0,
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
            friction: 0.5,
            restitution: 0.0,
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
            friction: 0.5,
            restitution: 0.0,
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
    .friction(desc.friction.max(0.0))
    .restitution(desc.restitution.max(0.0))
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
        .friction(desc.friction.max(0.0))
        .restitution(desc.restitution.max(0.0))
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
        .friction(desc.friction.max(0.0))
        .restitution(desc.restitution.max(0.0))
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

pub fn create_trimesh_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    vertices: &[f32],
    indices: &[u32],
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    let points = slice_to_points(vertices);
    let Some(triangles) = slice_to_triangles(indices) else {
        return RapierUnityColliderHandle::INVALID;
    };

    if points.is_empty() || triangles.is_empty() {
        return RapierUnityColliderHandle::INVALID;
    }

    let Ok(builder) = ColliderBuilder::trimesh(points, triangles) else {
        return RapierUnityColliderHandle::INVALID;
    };

    attach_collider(world, body, desc.apply(builder))
}

pub fn create_convex_hull_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    vertices: &[f32],
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    let points = slice_to_points(vertices);
    if points.is_empty() {
        return RapierUnityColliderHandle::INVALID;
    }

    let Some(builder) = ColliderBuilder::convex_hull(&points) else {
        return RapierUnityColliderHandle::INVALID;
    };

    attach_collider(world, body, desc.apply(builder))
}

pub fn create_heightfield_collider(
    world: &mut RapierUnityWorld,
    body: RapierUnityRigidBodyHandle,
    heights: &[f32],
    rows: usize,
    columns: usize,
    scale: RapierUnityVector3,
    desc: RapierUnityMeshColliderDesc,
) -> RapierUnityColliderHandle {
    if rows == 0 || columns == 0 || heights.len() != rows * columns {
        return RapierUnityColliderHandle::INVALID;
    }

    let matrix = DMatrix::from_row_slice(rows, columns, heights);
    let builder = ColliderBuilder::heightfield(matrix, Vector::new(scale.x, scale.y, scale.z));

    attach_collider(world, body, desc.apply(builder))
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

/// Runs `f` against the collider referenced by `collider`, returning `true` when it exists.
fn with_collider_mut(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    f: impl FnOnce(&mut Collider),
) -> bool {
    if !collider.is_valid() {
        return false;
    }

    if let Some(collider) = world.colliders.get_mut(collider.into()) {
        f(collider);
        true
    } else {
        false
    }
}

/// Reads a value from the collider referenced by `collider`, returning `None` when missing.
fn map_collider<T>(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    f: impl FnOnce(&Collider) -> T,
) -> Option<T> {
    if !collider.is_valid() {
        return None;
    }

    world.colliders.get(collider.into()).map(f)
}

/// Decodes the JS-style packed collision-groups value (memberships in the high
/// 16 bits, filter in the low 16 bits) into Rapier `InteractionGroups`.
pub(crate) fn decode_groups(value: u32) -> InteractionGroups {
    InteractionGroups::new(
        Group::from_bits_retain(value >> 16),
        Group::from_bits_retain(value & 0xFFFF),
    )
}

/// Encodes Rapier `InteractionGroups` back into the JS-style packed value.
fn encode_groups(groups: InteractionGroups) -> u32 {
    ((groups.memberships.bits() & 0xFFFF) << 16) | (groups.filter.bits() & 0xFFFF)
}

fn combine_rule_from_u32(value: u32) -> Option<CoefficientCombineRule> {
    match value {
        0 => Some(CoefficientCombineRule::Average),
        1 => Some(CoefficientCombineRule::Min),
        2 => Some(CoefficientCombineRule::Multiply),
        3 => Some(CoefficientCombineRule::Max),
        _ => None,
    }
}

fn combine_rule_to_u32(rule: CoefficientCombineRule) -> u32 {
    match rule {
        CoefficientCombineRule::Average => 0,
        CoefficientCombineRule::Min => 1,
        CoefficientCombineRule::Multiply => 2,
        CoefficientCombineRule::Max => 3,
    }
}

pub fn get_collider_friction(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<f32> {
    map_collider(world, collider, |collider| collider.friction())
}

pub fn set_collider_friction(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    friction: f32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_friction(friction.max(0.0))
    })
}

pub fn get_collider_restitution(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<f32> {
    map_collider(world, collider, |collider| collider.restitution())
}

pub fn set_collider_restitution(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    restitution: f32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_restitution(restitution.max(0.0))
    })
}

pub fn get_collider_friction_combine_rule(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| {
        combine_rule_to_u32(collider.friction_combine_rule())
    })
}

pub fn set_collider_friction_combine_rule(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    rule: u32,
) -> bool {
    let Some(rule) = combine_rule_from_u32(rule) else {
        return false;
    };

    with_collider_mut(world, collider, |collider| {
        collider.set_friction_combine_rule(rule)
    })
}

pub fn get_collider_restitution_combine_rule(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| {
        combine_rule_to_u32(collider.restitution_combine_rule())
    })
}

pub fn set_collider_restitution_combine_rule(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    rule: u32,
) -> bool {
    let Some(rule) = combine_rule_from_u32(rule) else {
        return false;
    };

    with_collider_mut(world, collider, |collider| {
        collider.set_restitution_combine_rule(rule)
    })
}

pub fn get_collider_collision_groups(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| {
        encode_groups(collider.collision_groups())
    })
}

pub fn set_collider_collision_groups(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    groups: u32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_collision_groups(decode_groups(groups))
    })
}

pub fn get_collider_solver_groups(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| {
        encode_groups(collider.solver_groups())
    })
}

pub fn set_collider_solver_groups(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    groups: u32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_solver_groups(decode_groups(groups))
    })
}

pub fn get_collider_sensor(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<bool> {
    map_collider(world, collider, |collider| collider.is_sensor())
}

pub fn set_collider_sensor(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    is_sensor: bool,
) -> bool {
    with_collider_mut(world, collider, |collider| collider.set_sensor(is_sensor))
}

pub fn get_collider_enabled(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<bool> {
    map_collider(world, collider, |collider| collider.is_enabled())
}

pub fn set_collider_enabled(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    enabled: bool,
) -> bool {
    with_collider_mut(world, collider, |collider| collider.set_enabled(enabled))
}

pub fn get_collider_density(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<f32> {
    map_collider(world, collider, |collider| collider.density())
}

pub fn set_collider_density(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    density: f32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_density(density.max(0.0))
    })
}

pub fn set_collider_translation_wrt_parent(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    translation: RapierUnityVector3,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_translation_wrt_parent(Vector::new(
            translation.x,
            translation.y,
            translation.z,
        ));
    })
}

pub fn set_collider_position_wrt_parent(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    transform: RapierUnityTransform,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_position_wrt_parent(transform.to_pose());
    })
}

pub fn get_collider_active_events(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| collider.active_events().bits())
}

pub fn set_collider_active_events(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    flags: u32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_active_events(ActiveEvents::from_bits_truncate(flags))
    })
}

pub fn get_collider_active_collision_types(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<u32> {
    map_collider(world, collider, |collider| {
        u32::from(collider.active_collision_types().bits())
    })
}

pub fn set_collider_active_collision_types(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    types: u32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_active_collision_types(ActiveCollisionTypes::from_bits_truncate(types as u16))
    })
}

pub fn get_collider_contact_force_event_threshold(
    world: &RapierUnityWorld,
    collider: RapierUnityColliderHandle,
) -> Option<f32> {
    map_collider(world, collider, |collider| {
        collider.contact_force_event_threshold()
    })
}

pub fn set_collider_contact_force_event_threshold(
    world: &mut RapierUnityWorld,
    collider: RapierUnityColliderHandle,
    threshold: f32,
) -> bool {
    with_collider_mut(world, collider, |collider| {
        collider.set_contact_force_event_threshold(threshold)
    })
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
