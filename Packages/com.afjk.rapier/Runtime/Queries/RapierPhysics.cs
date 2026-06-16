using System;
using UnityEngine;

namespace AFJK.Rapier
{
    public static class RapierPhysics
    {
        public static bool Raycast(
            RapierWorld world,
            Ray ray,
            float maxDistance,
            out RapierRaycastHit hit)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            return world.Raycast(ray, maxDistance, out hit);
        }

        public static bool Raycast(
            RapierWorldComponent worldComponent,
            Ray ray,
            float maxDistance,
            out RapierRaycastHit hit)
        {
            if (worldComponent == null)
            {
                throw new ArgumentNullException(nameof(worldComponent));
            }

            return worldComponent.EnsureWorld().Raycast(ray, maxDistance, out hit);
        }
    }
}

