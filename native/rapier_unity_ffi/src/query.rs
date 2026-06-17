use rapier3d::prelude::*;

use crate::collider::decode_groups;
use crate::handles::{RapierUnityColliderHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

/// FFI mirror of Rapier's `QueryFilter`. `flags` carries `QueryFilterFlags`
/// bits directly; the `use_*` bytes gate the optional fields.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityQueryFilter {
    pub flags: u32,
    pub use_groups: u8,
    pub groups: u32,
    pub use_exclude_collider: u8,
    pub exclude_collider: RapierUnityColliderHandle,
    pub use_exclude_body: u8,
    pub exclude_body: RapierUnityRigidBodyHandle,
}

impl Default for RapierUnityQueryFilter {
    fn default() -> Self {
        Self {
            flags: 0,
            use_groups: 0,
            groups: 0,
            use_exclude_collider: 0,
            exclude_collider: RapierUnityColliderHandle::INVALID,
            use_exclude_body: 0,
            exclude_body: RapierUnityRigidBodyHandle::INVALID,
        }
    }
}

impl RapierUnityQueryFilter {
    fn to_query_filter(self) -> QueryFilter<'static> {
        let mut filter = QueryFilter {
            flags: QueryFilterFlags::from_bits_truncate(self.flags),
            ..QueryFilter::default()
        };

        if self.use_groups != 0 {
            filter.groups = Some(decode_groups(self.groups));
        }

        if self.use_exclude_collider != 0 && self.exclude_collider.is_valid() {
            filter.exclude_collider = Some(self.exclude_collider.into());
        }

        if self.use_exclude_body != 0 && self.exclude_body.is_valid() {
            filter.exclude_rigid_body = Some(self.exclude_body.into());
        }

        filter
    }
}

/// Result of projecting a point onto the closest collider.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityPointProjection {
    pub collider: RapierUnityColliderHandle,
    pub point_x: f32,
    pub point_y: f32,
    pub point_z: f32,
    pub is_inside: u8,
}

impl Default for RapierUnityPointProjection {
    fn default() -> Self {
        Self {
            collider: RapierUnityColliderHandle::INVALID,
            point_x: 0.0,
            point_y: 0.0,
            point_z: 0.0,
            is_inside: 0,
        }
    }
}

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

/// Normalizes a ray's direction, returning `None` for degenerate inputs.
fn normalized_ray(ray: RapierUnityRay) -> Option<Ray> {
    let origin = Point::new(ray.origin_x, ray.origin_y, ray.origin_z);
    let direction = Vector::new(ray.direction_x, ray.direction_y, ray.direction_z);
    let direction_length = direction.norm();

    if !direction_length.is_finite() || direction_length <= f32::EPSILON {
        return None;
    }

    Some(Ray::new(origin, direction / direction_length))
}

pub fn raycast(
    world: &RapierUnityWorld,
    ray: RapierUnityRay,
    max_toi: f32,
) -> Option<RapierUnityRaycastHit> {
    raycast_filtered(world, ray, max_toi, true, RapierUnityQueryFilter::default())
}

pub fn raycast_filtered(
    world: &RapierUnityWorld,
    ray: RapierUnityRay,
    max_toi: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
) -> Option<RapierUnityRaycastHit> {
    if !max_toi.is_finite() || max_toi < 0.0 {
        return None;
    }

    let rapier_ray = normalized_ray(ray)?;
    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    query_pipeline
        .cast_ray_and_get_normal(&rapier_ray, max_toi, solid)
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

/// Projects a point onto the closest collider.
///
/// Scene queries operate against the broad-phase BVH as it was after the most
/// recent [`RapierUnityWorld::step`]; call `step` at least once before querying.
///
/// parry's `project_local_point` calls `.unwrap()` and panics (aborting across
/// the FFI boundary) whenever no collider passes the query — either because the
/// filter excludes every collider, or because the broad-phase BVH has not been
/// populated by a step yet. We defend against both: a pre-filter pass returns
/// `None` cleanly when nothing matches the filter, and a `catch_unwind` backstop
/// turns any remaining panic (e.g. an un-stepped BVH) into `None` instead of a
/// host-process abort.
pub fn project_point(
    world: &RapierUnityWorld,
    point_x: f32,
    point_y: f32,
    point_z: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
) -> Option<RapierUnityPointProjection> {
    let query_filter = filter.to_query_filter();

    let any_match = world
        .colliders
        .iter()
        .any(|(handle, collider)| query_filter.test(&world.bodies, handle, collider));
    if !any_match {
        return None;
    }

    let point = Point::new(point_x, point_y, point_z);
    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        query_filter,
    );

    let projection = std::panic::catch_unwind(std::panic::AssertUnwindSafe(|| {
        query_pipeline.project_point(&point, Real::MAX, solid)
    }))
    .ok()
    .flatten();

    projection.map(|(collider, projection)| RapierUnityPointProjection {
        collider: collider.into(),
        point_x: projection.point.x,
        point_y: projection.point.y,
        point_z: projection.point.z,
        is_inside: u8::from(projection.is_inside),
    })
}

pub fn intersection_with_point(
    world: &RapierUnityWorld,
    point_x: f32,
    point_y: f32,
    point_z: f32,
    filter: RapierUnityQueryFilter,
) -> Option<RapierUnityColliderHandle> {
    let point = Point::new(point_x, point_y, point_z);
    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    // Bind the iterator to a named local so it outlives the borrow of
    // `query_pipeline` until the resulting handle is produced.
    let mut hits = query_pipeline.intersect_point(point);
    hits.next().map(|(collider, _)| collider.into())
}
