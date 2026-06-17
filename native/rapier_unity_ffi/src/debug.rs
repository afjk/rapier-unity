use rapier3d::pipeline::{DebugColor, DebugRenderBackend, DebugRenderObject, DebugRenderPipeline};
use rapier3d::prelude::*;

use crate::world::RapierUnityWorld;

/// A single debug-render vertex.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct RapierUnityDebugVertex {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

/// An RGBA debug-render color (as produced by Rapier's debug-render style).
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct RapierUnityDebugColor {
    pub r: f32,
    pub g: f32,
    pub b: f32,
    pub a: f32,
}

/// Collects the line segments emitted by Rapier's debug-render pipeline.
#[derive(Default)]
struct LineCollector {
    vertices: Vec<RapierUnityDebugVertex>,
    colors: Vec<RapierUnityDebugColor>,
}

impl DebugRenderBackend for LineCollector {
    fn draw_line(
        &mut self,
        _object: DebugRenderObject,
        a: Point<Real>,
        b: Point<Real>,
        color: DebugColor,
    ) {
        self.vertices.push(RapierUnityDebugVertex {
            x: a.x,
            y: a.y,
            z: a.z,
        });
        self.vertices.push(RapierUnityDebugVertex {
            x: b.x,
            y: b.y,
            z: b.z,
        });
        self.colors.push(RapierUnityDebugColor {
            r: color[0],
            g: color[1],
            b: color[2],
            a: color[3],
        });
    }
}

/// Renders the world's debug geometry, writing line endpoints into
/// `out_vertices` (two per line) and per-line colors into `out_colors`. Returns
/// the number of lines written, capped by both buffers.
pub fn debug_render(
    world: &RapierUnityWorld,
    out_vertices: &mut [RapierUnityDebugVertex],
    out_colors: &mut [RapierUnityDebugColor],
) -> usize {
    let mut collector = LineCollector::default();
    let mut pipeline = DebugRenderPipeline::default();

    pipeline.render(
        &mut collector,
        &world.bodies,
        &world.colliders,
        &world.impulse_joints,
        &world.multibody_joints,
        &world.narrow_phase,
    );

    let line_count = out_colors.len().min(out_vertices.len() / 2);
    let count = line_count.min(collector.colors.len());

    out_vertices[..count * 2].copy_from_slice(&collector.vertices[..count * 2]);
    out_colors[..count].copy_from_slice(&collector.colors[..count]);

    count
}
