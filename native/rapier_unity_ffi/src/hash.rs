use rapier3d::prelude::*;

use crate::world::RapierUnityWorld;

const FNV_OFFSET: u64 = 0xcbf29ce484222325;
const FNV_PRIME: u64 = 0x100000001b3;

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

    fn write_f32(&mut self, value: f32) {
        self.write_u32(canonical_f32_bits(value));
    }

    fn write_vec3(&mut self, value: Vector) {
        self.write_f32(value.x);
        self.write_f32(value.y);
        self.write_f32(value.z);
    }

    fn write_pose(&mut self, value: &Pose) {
        let translation = value.translation;
        let rotation = value.rotation;

        self.write_f32(translation.x);
        self.write_f32(translation.y);
        self.write_f32(translation.z);
        self.write_f32(rotation.x);
        self.write_f32(rotation.y);
        self.write_f32(rotation.z);
        self.write_f32(rotation.w);
    }

    fn write_handle(&mut self, index: u32, generation: u32) {
        self.write_u32(index);
        self.write_u32(generation);
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

pub fn world_state_hash(world: &RapierUnityWorld) -> u64 {
    let mut hasher = StableHasher::new();

    hasher.write_f32(world.gravity.x);
    hasher.write_f32(world.gravity.y);
    hasher.write_f32(world.gravity.z);
    hasher.write_f32(world.integration_parameters.dt);

    let mut bodies: Vec<_> = world.bodies.iter().collect();
    bodies.sort_by_key(|(handle, _)| handle.into_raw_parts());

    hasher.write_u64(bodies.len() as u64);
    for (handle, body) in bodies {
        let (index, generation) = handle.into_raw_parts();
        hasher.write_handle(index, generation);
        hasher.write_u8(body_type_id(body.body_type()));
        hasher.write_pose(body.position());
        hasher.write_vec3(body.linvel());
        hasher.write_vec3(body.angvel());
        hasher.write_u8(u8::from(body.is_sleeping()));
        hasher.write_u8(u8::from(body.is_enabled()));
    }

    let mut colliders: Vec<_> = world.colliders.iter().collect();
    colliders.sort_by_key(|(handle, _)| handle.into_raw_parts());

    hasher.write_u64(colliders.len() as u64);
    for (handle, collider) in colliders {
        let (index, generation) = handle.into_raw_parts();
        hasher.write_handle(index, generation);
        hasher.write_pose(collider.position());
        hasher.write_f32(collider.density());
        hasher.write_u8(u8::from(collider.is_sensor()));
        hasher.write_u8(u8::from(collider.is_enabled()));
    }

    hasher.finish()
}
