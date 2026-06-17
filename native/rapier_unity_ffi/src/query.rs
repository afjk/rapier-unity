use rapier3d::parry::query::{ShapeCastOptions, ShapeCastStatus};
use rapier3d::prelude::*;

use crate::body::{RapierUnityTransform, RapierUnityVector3};
use crate::collider::decode_groups;
use crate::handles::{RapierUnityColliderHandle, RapierUnityRigidBodyHandle};
use crate::world::RapierUnityWorld;

/// Primitive shape kinds usable for shape-cast and shape-intersection queries.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityQueryShape {
    /// 0 = ball, 1 = cuboid, 2 = capsule (Y axis).
    pub shape_type: u32,
    pub half_extents_x: f32,
    pub half_extents_y: f32,
    pub half_extents_z: f32,
    pub radius: f32,
    pub half_height: f32,
}

impl RapierUnityQueryShape {
    fn to_shared_shape(self) -> Option<SharedShape> {
        match self.shape_type {
            0 => Some(SharedShape::ball(self.radius.max(0.0))),
            1 => Some(SharedShape::cuboid(
                self.half_extents_x.max(0.0),
                self.half_extents_y.max(0.0),
                self.half_extents_z.max(0.0),
            )),
            2 => Some(SharedShape::capsule_y(
                self.half_height.max(0.0),
                self.radius.max(0.0),
            )),
            _ => None,
        }
    }
}

/// Result of a shape cast against the closest collider.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct RapierUnityShapeCastHit {
    pub collider: RapierUnityColliderHandle,
    pub time_of_impact: f32,
    pub witness1_x: f32,
    pub witness1_y: f32,
    pub witness1_z: f32,
    pub witness2_x: f32,
    pub witness2_y: f32,
    pub witness2_z: f32,
    pub normal1_x: f32,
    pub normal1_y: f32,
    pub normal1_z: f32,
    pub normal2_x: f32,
    pub normal2_y: f32,
    pub normal2_z: f32,
    /// 0 = out of iterations, 1 = converged, 2 = failed, 3 = penetrating/within target distance.
    pub status: u32,
}

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

/// Casts all colliders intersecting a ray into `out`, returning the number of
/// hits written (capped at `out.len()`). Order is unspecified.
pub fn raycast_all(
    world: &RapierUnityWorld,
    ray: RapierUnityRay,
    max_toi: f32,
    solid: bool,
    filter: RapierUnityQueryFilter,
    out: &mut [RapierUnityRaycastHit],
) -> usize {
    if out.is_empty() || !max_toi.is_finite() || max_toi < 0.0 {
        return 0;
    }

    let Some(rapier_ray) = normalized_ray(ray) else {
        return 0;
    };

    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    let mut count = 0;
    for (collider, _, intersection) in query_pipeline.intersect_ray(rapier_ray, max_toi, solid) {
        if count >= out.len() {
            break;
        }

        let point = rapier_ray.point_at(intersection.time_of_impact);
        out[count] = RapierUnityRaycastHit {
            collider: collider.into(),
            point_x: point.x,
            point_y: point.y,
            point_z: point.z,
            normal_x: intersection.normal.x,
            normal_y: intersection.normal.y,
            normal_z: intersection.normal.z,
            toi: intersection.time_of_impact,
        };
        count += 1;
    }

    count
}

pub fn cast_shape(
    world: &RapierUnityWorld,
    shape_pos: RapierUnityTransform,
    shape_vel: RapierUnityVector3,
    shape: RapierUnityQueryShape,
    max_toi: f32,
    stop_at_penetration: bool,
    filter: RapierUnityQueryFilter,
) -> Option<RapierUnityShapeCastHit> {
    if !max_toi.is_finite() || max_toi < 0.0 {
        return None;
    }

    let shared_shape = shape.to_shared_shape()?;
    let pose = shape_pos.to_pose();
    let velocity = Vector::new(shape_vel.x, shape_vel.y, shape_vel.z);
    let options = ShapeCastOptions {
        max_time_of_impact: max_toi,
        stop_at_penetration,
        ..ShapeCastOptions::default()
    };

    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    query_pipeline
        .cast_shape(&pose, &velocity, shared_shape.as_ref(), options)
        .map(|(collider, hit)| {
            let status = match hit.status {
                ShapeCastStatus::OutOfIterations => 0,
                ShapeCastStatus::Converged => 1,
                ShapeCastStatus::Failed => 2,
                ShapeCastStatus::PenetratingOrWithinTargetDist => 3,
            };

            RapierUnityShapeCastHit {
                collider: collider.into(),
                time_of_impact: hit.time_of_impact,
                witness1_x: hit.witness1.x,
                witness1_y: hit.witness1.y,
                witness1_z: hit.witness1.z,
                witness2_x: hit.witness2.x,
                witness2_y: hit.witness2.y,
                witness2_z: hit.witness2.z,
                normal1_x: hit.normal1.x,
                normal1_y: hit.normal1.y,
                normal1_z: hit.normal1.z,
                normal2_x: hit.normal2.x,
                normal2_y: hit.normal2.y,
                normal2_z: hit.normal2.z,
                status,
            }
        })
}

/// Writes the handles of all colliders intersecting `shape` at `shape_pos` into
/// `out`, returning the number written (capped at `out.len()`).
pub fn intersect_shape(
    world: &RapierUnityWorld,
    shape_pos: RapierUnityTransform,
    shape: RapierUnityQueryShape,
    filter: RapierUnityQueryFilter,
    out: &mut [RapierUnityColliderHandle],
) -> usize {
    if out.is_empty() {
        return 0;
    }

    let Some(shared_shape) = shape.to_shared_shape() else {
        return 0;
    };
    let pose = shape_pos.to_pose();

    let query_pipeline = world.broad_phase.as_query_pipeline(
        world.narrow_phase.query_dispatcher(),
        &world.bodies,
        &world.colliders,
        filter.to_query_filter(),
    );

    let mut count = 0;
    for (collider, _) in query_pipeline.intersect_shape(pose, shared_shape.as_ref()) {
        if count >= out.len() {
            break;
        }

        out[count] = collider.into();
        count += 1;
    }

    count
}
