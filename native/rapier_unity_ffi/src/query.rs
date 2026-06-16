use rapier3d::prelude::*;

use crate::handles::RapierUnityColliderHandle;
use crate::world::RapierUnityWorld;

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityRay {
    pub origin_x: f32,
    pub origin_y: f32,
    pub origin_z: f32,
    pub direction_x: f32,
    pub direction_y: f32,
    pub direction_z: f32,
}

#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityRaycastHit {
    pub collider: RapierUnityColliderHandle,
    pub point_x: f32,
    pub point_y: f32,
    pub point_z: f32,
    pub normal_x: f32,
    pub normal_y: f32,
    pub normal_z: f32,
    pub toi: f32,
}

impl Default for RapierUnityRaycastHit {
    fn default() -> Self {
        Self {
            collider: RapierUnityColliderHandle::INVALID,
            point_x: 0.0,
            point_y: 0.0,
            point_z: 0.0,
            normal_x: 0.0,
            normal_y: 0.0,
            normal_z: 0.0,
            toi: 0.0,
        }
    }
}

pub fn raycast(
    world: &RapierUnityWorld,
    ray: RapierUnityRay,
    max_toi: f32,
) -> Option<RapierUnityRaycastHit> {
    if !max_toi.is_finite() || max_toi < 0.0 {
        return None;
    }

    let origin = Point::new(ray.origin_x, ray.origin_y, ray.origin_z);
    let direction = Vector::new(ray.direction_x, ray.direction_y, ray.direction_z);
    let direction_length = direction.norm();

    if !direction_length.is_finite() || direction_length <= f32::EPSILON {
        return None;
    }

    let direction = direction / direction_length;
    let rapier_ray = Ray::new(origin, direction);
    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        QueryFilter::default(),
    );

    query_pipeline
        .cast_ray_and_get_normal(&rapier_ray, max_toi, true)
        .map(|(collider, hit)| {
            let point = rapier_ray.point_at(hit.time_of_impact);

            RapierUnityRaycastHit {
                collider: collider.into(),
                point_x: point.x,
                point_y: point.y,
                point_z: point.z,
                normal_x: hit.normal.x,
                normal_y: hit.normal.y,
                normal_z: hit.normal.z,
                toi: hit.time_of_impact,
            }
        })
}
