using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Helpers for drawing a <see cref="RapierWorld"/>'s native debug geometry.
    /// </summary>
    public static class RapierDebugRenderer
    {
        /// <summary>
        /// Renders the world's debug lines with <see cref="Debug.DrawLine(Vector3, Vector3, Color, float)"/>.
        /// The buffers are (re)allocated to fit <paramref name="maxLines"/> and reused across
        /// calls; pass them back in each frame to avoid per-frame allocation.
        /// </summary>
        /// <returns>The number of lines drawn.</returns>
        public static int DrawRuntimeLines(
            RapierWorld world,
            ref Vector3[] vertices,
            ref Color[] colors,
            int maxLines = 4096,
            float duration = 0f)
        {
            if (world == null || !world.IsCreated || maxLines <= 0)
            {
                return 0;
            }

            if (colors == null || colors.Length < maxLines)
            {
                colors = new Color[maxLines];
            }

            if (vertices == null || vertices.Length < maxLines * 2)
            {
                vertices = new Vector3[maxLines * 2];
            }

            var lines = world.DebugRender(vertices, colors);
            for (var i = 0; i < lines; i++)
            {
                Debug.DrawLine(vertices[i * 2], vertices[(i * 2) + 1], colors[i], duration);
            }

            return lines;
        }
    }
}
