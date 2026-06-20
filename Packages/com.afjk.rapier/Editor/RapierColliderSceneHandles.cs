using AFJK.Rapier;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    /// <summary>
    /// Draws Unity-style Scene View handles (wire shapes + edit handles) for the supported
    /// Rapier collider types. Editor-only; this never touches runtime/serialized field names.
    ///
    /// Transform model used for placement:
    ///   GameObject Transform + Collider localPosition + Collider localRotation + shape size,
    /// with handle sizes expressed in world units so they account for Transform.lossyScale,
    /// matching Unity's collider authoring convention.
    /// </summary>
    internal static class RapierColliderSceneHandles
    {
        // Reused across OnSceneGUI calls so Unity keeps a single hot control per handle.
        private static readonly BoxBoundsHandle BoxHandle = new BoxBoundsHandle();
        private static readonly SphereBoundsHandle SphereHandle = new SphereBoundsHandle();
        private static readonly CapsuleBoundsHandle CapsuleHandle = new CapsuleBoundsHandle();

        // Guards size <-> scale conversions against zero/near-zero scale axes.
        private static float SafeScale(float v) => Mathf.Abs(v) > 1e-6f ? Mathf.Abs(v) : 1f;

        public static void DrawBox(SerializedObject serializedObject, RapierBoxCollider collider)
        {
            serializedObject.Update();

            var localPositionProp = serializedObject.FindProperty("localPosition");
            var localRotationProp = serializedObject.FindProperty("localRotation");
            var halfExtentsProp = serializedObject.FindProperty("halfExtents");
            if (localPositionProp == null || halfExtentsProp == null)
            {
                return;
            }

            var transform = collider.transform;
            var lossyScale = transform.lossyScale;
            var scaleX = SafeScale(lossyScale.x);
            var scaleY = SafeScale(lossyScale.y);
            var scaleZ = SafeScale(lossyScale.z);

            var localPosition = localPositionProp.vector3Value;
            var localRotation = NormalizeRotation(localRotationProp);
            var halfExtents = halfExtentsProp.vector3Value;

            var matrix = ShapeMatrix(transform, localPosition, localRotation);
            using (new Handles.DrawingScope(matrix))
            {
                // size is in world units; the matrix carries no scale, so we apply lossyScale here.
                BoxHandle.center = Vector3.zero;
                BoxHandle.size = new Vector3(
                    halfExtents.x * 2f * scaleX,
                    halfExtents.y * 2f * scaleY,
                    halfExtents.z * 2f * scaleZ);

                EditorGUI.BeginChangeCheck();
                BoxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    var worldCenter = matrix.MultiplyPoint3x4(BoxHandle.center);
                    localPositionProp.vector3Value =
                        SafeLocalCenter(transform, worldCenter, localPositionProp.vector3Value);

                    var size = BoxHandle.size;
                    halfExtentsProp.vector3Value = new Vector3(
                        Mathf.Max(0f, size.x / scaleX * 0.5f),
                        Mathf.Max(0f, size.y / scaleY * 0.5f),
                        Mathf.Max(0f, size.z / scaleZ * 0.5f));

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public static void DrawSphere(SerializedObject serializedObject, RapierSphereCollider collider)
        {
            serializedObject.Update();

            var localPositionProp = serializedObject.FindProperty("localPosition");
            var radiusProp = serializedObject.FindProperty("radius");
            if (localPositionProp == null || radiusProp == null)
            {
                return;
            }

            var transform = collider.transform;
            var lossyScale = transform.lossyScale;
            // Unity's SphereCollider scales the radius by the largest absolute axis scale.
            var maxScale = Mathf.Max(SafeScale(lossyScale.x), SafeScale(lossyScale.y), SafeScale(lossyScale.z));

            var localPosition = localPositionProp.vector3Value;

            var matrix = ShapeMatrix(transform, localPosition, Quaternion.identity);
            using (new Handles.DrawingScope(matrix))
            {
                SphereHandle.center = Vector3.zero;
                SphereHandle.radius = radiusProp.floatValue * maxScale;

                EditorGUI.BeginChangeCheck();
                SphereHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    var worldCenter = matrix.MultiplyPoint3x4(SphereHandle.center);
                    localPositionProp.vector3Value =
                        SafeLocalCenter(transform, worldCenter, localPositionProp.vector3Value);

                    radiusProp.floatValue = Mathf.Max(0f, SphereHandle.radius / maxScale);

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public static void DrawCapsule(SerializedObject serializedObject, RapierCapsuleCollider collider)
        {
            serializedObject.Update();

            var localPositionProp = serializedObject.FindProperty("localPosition");
            var localRotationProp = serializedObject.FindProperty("localRotation");
            var radiusProp = serializedObject.FindProperty("radius");
            var halfHeightProp = serializedObject.FindProperty("halfHeight");
            if (localPositionProp == null || radiusProp == null || halfHeightProp == null)
            {
                return;
            }

            var transform = collider.transform;
            var lossyScale = transform.lossyScale;
            // Rapier capsules are aligned to their local Y axis (matches Unity's default mental model).
            var heightScale = SafeScale(lossyScale.y);
            var radiusScale = Mathf.Max(SafeScale(lossyScale.x), SafeScale(lossyScale.z));

            var localPosition = localPositionProp.vector3Value;
            var localRotation = NormalizeRotation(localRotationProp);

            var matrix = ShapeMatrix(transform, localPosition, localRotation);
            using (new Handles.DrawingScope(matrix))
            {
                CapsuleHandle.heightAxis = CapsuleBoundsHandle.HeightAxis.Y;
                CapsuleHandle.center = Vector3.zero;
                CapsuleHandle.radius = radiusProp.floatValue * radiusScale;
                // Inspector shows Height = halfHeight * 2; keep the handle consistent with it.
                CapsuleHandle.height = halfHeightProp.floatValue * 2f * heightScale;

                EditorGUI.BeginChangeCheck();
                CapsuleHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    var worldCenter = matrix.MultiplyPoint3x4(CapsuleHandle.center);
                    localPositionProp.vector3Value =
                        SafeLocalCenter(transform, worldCenter, localPositionProp.vector3Value);

                    radiusProp.floatValue = Mathf.Max(0f, CapsuleHandle.radius / radiusScale);
                    halfHeightProp.floatValue = Mathf.Max(0f, CapsuleHandle.height / heightScale * 0.5f);

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        // Builds a scale-free matrix at the collider's authored placement. lossyScale is applied to
        // the handle size values directly so spheres stay round and handle dots keep a uniform size.
        private static Matrix4x4 ShapeMatrix(Transform transform, Vector3 localPosition, Quaternion localRotation)
        {
            var worldCenter = transform.TransformPoint(localPosition);
            var rotation = transform.rotation * localRotation;
            return Matrix4x4.TRS(worldCenter, rotation, Vector3.one);
        }

        // Converts the handle's world center back to collider-local space. When a Transform axis has
        // zero scale the localToWorld matrix is singular and InverseTransformPoint yields NaN/Inf, so
        // we keep the previous value instead of serializing a corrupt (NaN) localPosition.
        private static Vector3 SafeLocalCenter(Transform transform, Vector3 worldCenter, Vector3 fallback)
        {
            var local = transform.InverseTransformPoint(worldCenter);
            if (IsFinite(local.x) && IsFinite(local.y) && IsFinite(local.z))
            {
                return local;
            }

            return fallback;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        private static Quaternion NormalizeRotation(SerializedProperty localRotationProp)
        {
            if (localRotationProp == null)
            {
                return Quaternion.identity;
            }

            // A zero/unset quaternion (e.g. legacy serialized data) isn't a valid rotation.
            var q = localRotationProp.quaternionValue;
            var sqrMagnitude = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return sqrMagnitude < 1e-6f ? Quaternion.identity : q;
        }
    }
}
