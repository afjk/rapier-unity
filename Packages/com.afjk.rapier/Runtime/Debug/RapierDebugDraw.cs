using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierDebugDraw : MonoBehaviour
    {
        [SerializeField] private RapierWorldBehaviour worldComponent;
        [SerializeField] private Color bodyColor = Color.cyan;
        [SerializeField] private float bodyMarkerRadius = 0.08f;

        private void Reset()
        {
            worldComponent = GetComponent<RapierWorldBehaviour>();
        }

        private void OnDrawGizmosSelected()
        {
            if (worldComponent == null)
            {
                worldComponent = GetComponent<RapierWorldBehaviour>();
            }

            if (worldComponent == null)
            {
                return;
            }

            var bodies = GetComponentsInChildren<RapierRigidbody>();
            Gizmos.color = bodyColor;
            for (var i = 0; i < bodies.Length; i++)
            {
                Gizmos.DrawWireSphere(bodies[i].transform.position, bodyMarkerRadius);
            }
        }
    }
}

