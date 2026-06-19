using System;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Component-friendly façade over the <see cref="RapierWorld"/> scene-query API. Every helper
    /// accepts either a raw <see cref="RapierWorld"/> or a <see cref="RapierWorldBehaviour"/>, so
    /// queries can be issued without reaching into the low-level handle types directly.
    /// </summary>
    public static class RapierPhysics
    {
        public static bool Raycast(
            RapierWorld world,
            Ray ray,
            float maxDistance,
            out RapierRaycastHit hit)
        {
            return Resolve(world).Raycast(ray, maxDistance, out hit);
        }

        public static bool Raycast(
            RapierWorldBehaviour worldComponent,
            Ray ray,
            float maxDistance,
            out RapierRaycastHit hit)
        {
            return Resolve(worldComponent).Raycast(ray, maxDistance, out hit);
        }

        public static bool RaycastFiltered(
            RapierWorldBehaviour worldComponent,
            Ray ray,
            float maxDistance,
            RapierQueryFilter filter,
            out RapierRaycastHit hit,
            bool solid = true)
        {
            return Resolve(worldComponent).RaycastFiltered(ray, maxDistance, solid, filter, out hit);
        }

        public static int RaycastAll(
            RapierWorldBehaviour worldComponent,
            Ray ray,
            float maxDistance,
            RapierQueryFilter filter,
            RapierRaycastHit[] results,
            bool solid = true)
        {
            return Resolve(worldComponent).RaycastAll(ray, maxDistance, solid, filter, results);
        }

        public static bool ProjectPoint(
            RapierWorldBehaviour worldComponent,
            Vector3 point,
            RapierQueryFilter filter,
            out RapierPointProjection projection,
            bool solid = true)
        {
            return Resolve(worldComponent).TryProjectPoint(point, solid, filter, out projection);
        }

        public static bool IntersectionWithPoint(
            RapierWorldBehaviour worldComponent,
            Vector3 point,
            RapierQueryFilter filter,
            out RapierColliderHandle collider)
        {
            return Resolve(worldComponent).TryIntersectionWithPoint(point, filter, out collider);
        }

        public static bool CastShape(
            RapierWorldBehaviour worldComponent,
            RapierTransform shapePosition,
            Vector3 shapeVelocity,
            RapierQueryShape shape,
            float maxDistance,
            RapierQueryFilter filter,
            out RapierShapeCastHit hit,
            bool stopAtPenetration = true)
        {
            return Resolve(worldComponent).CastShape(
                shapePosition,
                shapeVelocity,
                shape,
                maxDistance,
                stopAtPenetration,
                filter,
                out hit);
        }

        public static int IntersectShape(
            RapierWorldBehaviour worldComponent,
            RapierTransform shapePosition,
            RapierQueryShape shape,
            RapierQueryFilter filter,
            RapierColliderHandle[] results)
        {
            return Resolve(worldComponent).IntersectShape(shapePosition, shape, filter, results);
        }

        private static RapierWorld Resolve(RapierWorld world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            return world;
        }

        private static RapierWorld Resolve(RapierWorldBehaviour worldComponent)
        {
            if (worldComponent == null)
            {
                throw new ArgumentNullException(nameof(worldComponent));
            }

            return worldComponent.EnsureWorld();
        }
    }
}
