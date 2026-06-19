using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Metadata attached by <see cref="RapierSceneImporter"/> to each imported GameObject. It keeps
    /// source-specific information (which external system an object came from and its id/order there)
    /// out of the core Rapier components, so those stay independent of any importer or network layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RapierImportedObject : MonoBehaviour
    {
        [SerializeField] private string sourceSystem = string.Empty;
        [SerializeField] private string sourceId = string.Empty;
        [SerializeField] private int sourceOrder;

        /// <summary>Name of the system this object was imported from (e.g. a Scene Sync adapter).</summary>
        public string SourceSystem
        {
            get => sourceSystem;
            set => sourceSystem = value ?? string.Empty;
        }

        /// <summary>The object's id in the source system (maps to the Rapier component StableId).</summary>
        public string SourceId
        {
            get => sourceId;
            set => sourceId = value ?? string.Empty;
        }

        /// <summary>The object's order in the source system (maps to the Rapier RegistrationOrder).</summary>
        public int SourceOrder
        {
            get => sourceOrder;
            set => sourceOrder = value;
        }
    }
}
