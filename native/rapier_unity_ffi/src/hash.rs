use rapier3d::math::{Isometry, Vector};
use rapier3d::parry::shape::TypedShape;
use rapier3d::prelude::*;

use crate::world::RapierUnityWorld;

const FNV_OFFSET: u64 = 0xcbf29ce484222325;
const FNV_PRIME: u64 = 0x100000001b3;
pub const RAPIER_CORE_VERSION: &str = "0.30.0";
const CANONICAL_HASH_NAME: &str = "SceneSyncCanonicalPhysicsHashV1";

struct StableHasher {
    value: u64,
}

impl StableHasher {
    fn new() -> Self {
        Self { value: FNV_OFFSET }
    }

    fn finish(self) -> u64 {
        self.value
    }

    fn write_u8(&mut self, value: u8) {
        self.value ^= u64::from(value);
        self.value = self.value.wrapping_mul(FNV_PRIME);
    }

    fn write_u32(&mut self, value: u32) {
        for byte in value.to_le_bytes() {
            self.write_u8(byte);
        }
    }

    fn write_u64(&mut self, value: u64) {
        for byte in value.to_le_bytes() {
            self.write_u8(byte);
        }
    }

    fn write_bytes(&mut self, bytes: &[u8]) {
        for byte in bytes {
            self.write_u8(*byte);
        }
    }

    fn write_str(&mut self, value: &str) {
        let bytes = value.as_bytes();
        self.write_u32(bytes.len() as u32);
        self.write_bytes(bytes);
    }

    fn write_f32(&mut self, value: f32) {
        self.write_u32(canonical_f32_bits(value));
    }

    fn write_vec3(&mut self, value: &Vector<Real>) {
        self.write_f32(value.x);
        self.write_f32(value.y);
        self.write_f32(value.z);
    }

    fn write_pose(&mut self, value: &Isometry<Real>) {
        let translation = value.translation.vector;
        let rotation = value.rotation.quaternion();

        self.write_f32(translation.x);
        self.write_f32(translation.y);
        self.write_f32(translation.z);
        self.write_f32(rotation.i);
        self.write_f32(rotation.j);
        self.write_f32(rotation.k);
        self.write_f32(rotation.w);
    }

    fn write_handle(&mut self, index: u32, generation: u32) {
        self.write_u32(index);
        self.write_u32(generation);
    }

    fn write_body_identity(&mut self, world: &RapierUnityWorld, handle: RigidBodyHandle) {
        if let Some(stable_id) = world.body_stable_ids.get(&handle) {
            self.write_u8(1);
            self.write_u64(*stable_id);
        } else {
            let (index, generation) = handle.into_raw_parts();
            self.write_u8(2);
            self.write_handle(index, generation);
        }
    }

    fn write_collider_identity(&mut self, world: &RapierUnityWorld, handle: ColliderHandle) {
        if let Some(stable_id) = world.collider_stable_ids.get(&handle) {
            self.write_u8(1);
            self.write_u64(*stable_id);
        } else {
            let (index, generation) = handle.into_raw_parts();
            self.write_u8(2);
            self.write_handle(index, generation);
        }
    }

    fn write_optional_rigid_body_identity(
        &mut self,
        world: &RapierUnityWorld,
        handle: Option<RigidBodyHandle>,
    ) {
        if let Some(handle) = handle {
            self.write_body_identity(world, handle);
        } else {
            self.write_u8(0);
        }
    }
}

fn canonical_f32_bits(value: f32) -> u32 {
    if value == 0.0 {
        0.0f32.to_bits()
    } else if value.is_nan() {
        f32::NAN.to_bits()
    } else {
        value.to_bits()
    }
}

fn body_type_id(body_type: RigidBodyType) -> u8 {
    match body_type {
        RigidBodyType::Dynamic => 0,
        RigidBodyType::Fixed => 1,
        RigidBodyType::KinematicPositionBased => 2,
        RigidBodyType::KinematicVelocityBased => 3,
    }
}

fn coefficient_combine_rule_id(rule: CoefficientCombineRule) -> u8 {
    match rule {
        CoefficientCombineRule::Average => 0,
        CoefficientCombineRule::Min => 1,
        CoefficientCombineRule::Multiply => 2,
        CoefficientCombineRule::Max => 3,
    }
}

pub fn stable_id_hash_bytes(bytes: &[u8]) -> u64 {
    let mut hasher = StableHasher::new();
    hasher.write_bytes(bytes);
    hasher.finish()
}

fn body_sort_key(world: &RapierUnityWorld, handle: RigidBodyHandle) -> (u8, u64, u32, u32) {
    let (index, generation) = handle.into_raw_parts();
    if let Some(stable_id) = world.body_stable_ids.get(&handle) {
        (0, *stable_id, index, generation)
    } else {
        (1, u64::from(index), generation, 0)
    }
}

fn collider_sort_key(world: &RapierUnityWorld, handle: ColliderHandle) -> (u8, u64, u32, u32) {
    let (index, generation) = handle.into_raw_parts();
    if let Some(stable_id) = world.collider_stable_ids.get(&handle) {
        (0, *stable_id, index, generation)
    } else {
        (1, u64::from(index), generation, 0)
    }
}

fn hash_collider_shape(hasher: &mut StableHasher, collider: &Collider) {
    match collider.shape().as_typed_shape() {
        TypedShape::Ball(ball) => {
            hasher.write_u8(1);
            hasher.write_f32(ball.radius);
        }
        TypedShape::Cuboid(cuboid) => {
            hasher.write_u8(2);
            hasher.write_vec3(&cuboid.half_extents);
        }
        TypedShape::Capsule(capsule) => {
            hasher.write_u8(3);
            hasher.write_vec3(&capsule.segment.a.coords);
            hasher.write_vec3(&capsule.segment.b.coords);
            hasher.write_f32(capsule.radius);
        }
        _ => {
            // Keep unknown shapes distinguishable until each shape gets a full stable descriptor.
            hasher.write_u8(255);
        }
    }
}

pub fn world_state_hash(world: &RapierUnityWorld) -> u64 {
    let mut hasher = StableHasher::new();

    hasher.write_str(CANONICAL_HASH_NAME);
    hasher.write_str("rapier");
    hasher.write_str(RAPIER_CORE_VERSION);
    hasher.write_f32(world.gravity.x);
    hasher.write_f32(world.gravity.y);
    hasher.write_f32(world.gravity.z);
    hasher.write_f32(world.integration_parameters.dt);

    let mut bodies: Vec<_> = world.bodies.iter().collect();
    bodies.sort_by_key(|(handle, _)| body_sort_key(world, *handle));

    hasher.write_u64(bodies.len() as u64);
    for (handle, body) in bodies {
        hasher.write_body_identity(world, handle);
        hasher.write_u8(body_type_id(body.body_type()));
        hasher.write_f32(body.linear_damping());
        hasher.write_f32(body.angular_damping());
        hasher.write_u64(body.additional_solver_iterations() as u64);
        hasher.write_u8(u8::from(body.is_ccd_enabled()));
        hasher.write_u8(u8::from(
            world.body_can_sleep.get(&handle).copied().unwrap_or(true),
        ));
        hasher.write_pose(body.position());
        hasher.write_vec3(body.linvel());
        hasher.write_vec3(body.angvel());
        hasher.write_u8(u8::from(body.is_sleeping()));
        hasher.write_u8(u8::from(body.is_enabled()));
    }

    let mut colliders: Vec<_> = world.colliders.iter().collect();
    colliders.sort_by_key(|(handle, _)| collider_sort_key(world, *handle));

    hasher.write_u64(colliders.len() as u64);
    for (handle, collider) in colliders {
        hasher.write_collider_identity(world, handle);
        hasher.write_optional_rigid_body_identity(world, collider.parent());
        if let Some(local_pose) = collider.position_wrt_parent() {
            hasher.write_u8(1);
            hasher.write_pose(local_pose);
        } else {
            hasher.write_u8(0);
        }
        hash_collider_shape(&mut hasher, collider);
        hasher.write_f32(collider.density());
        hasher.write_f32(collider.friction());
        hasher.write_u8(coefficient_combine_rule_id(
            collider.friction_combine_rule(),
        ));
        hasher.write_f32(collider.restitution());
        hasher.write_u8(coefficient_combine_rule_id(
            collider.restitution_combine_rule(),
        ));
        hasher.write_u8(u8::from(collider.is_sensor()));
        hasher.write_u8(u8::from(collider.is_enabled()));
    }

    hasher.finish()
}
