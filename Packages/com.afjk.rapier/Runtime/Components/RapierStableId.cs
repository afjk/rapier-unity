using System.Text;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Helpers for producing stable identifiers for Rapier components.
    /// <para>
    /// Two strategies are provided: <see cref="Generate"/> creates a fresh random id (suitable for
    /// editor-time assignment that is then serialized into a Scene/Prefab), and
    /// <see cref="FromHierarchy"/> derives a deterministic id from a transform's hierarchy path
    /// (suitable for runtime/procedural worlds where the same hierarchy must map to the same id on
    /// every host without serialization).
    /// </para>
    /// </summary>
    public static class RapierStableId
    {
        /// <summary>Creates a fresh, unique id (a 32-character hex GUID).</summary>
        public static string Generate()
        {
            return System.Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Builds a deterministic id from <paramref name="transform"/>'s full hierarchy path. Each
        /// path segment includes the sibling index so same-named siblings stay distinct, and an
        /// optional <paramref name="discriminator"/> separates multiple components on one object.
        /// The same hierarchy structure always yields the same id, on any host.
        /// </summary>
        public static string FromHierarchy(Transform transform, string discriminator = null)
        {
            if (transform == null)
            {
                return string.IsNullOrEmpty(discriminator) ? string.Empty : "#" + discriminator;
            }

            var builder = new StringBuilder();
            AppendPath(builder, transform);
            if (!string.IsNullOrEmpty(discriminator))
            {
                builder.Append('#').Append(discriminator);
            }

            return builder.ToString();
        }

        private static void AppendPath(StringBuilder builder, Transform transform)
        {
            if (transform.parent != null)
            {
                AppendPath(builder, transform.parent);
                builder.Append('/');
            }

            builder.Append(transform.name).Append('[').Append(transform.GetSiblingIndex()).Append(']');
        }
    }
}
