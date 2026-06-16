using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierDebugDraw : MonoBehaviour
    {
        [SerializeField] private RapierWorldComponent worldComponent;
        [SerializeField] private Color bodyColor = Color.cyan;
        [SerializeField] private float bodyMarkerRadius = 0.08f;

        private void Reset()
        {
            worldComponent = GetComponent<RapierWorldComponent>();
        }

        private void OnDrawGizmosSelected()
        {
            if (worldComponent == null)
            {
                worldComponent = GetComponent<RapierWorldComponent>();
            }

            if (worldComponent == null)
            {
                return;
            }

            var bodies = GetComponentsInChildren<RapierRigidBodyComponent>();
            Gizmos.color = bodyColor;
            for (var i = 0; i < bodies.Length; i++)
            {
                Gizmos.DrawWireSphere(bodies[i].transform.position, bodyMarkerRadius);
            }
        }
    }
}

