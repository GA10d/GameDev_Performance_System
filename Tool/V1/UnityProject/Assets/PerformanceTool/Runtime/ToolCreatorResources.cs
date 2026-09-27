using UnityEngine;

namespace Astra.PerformanceTool
{
    [CreateAssetMenu(menuName="Astra/Performance Tool/Creator Resources")]
    public sealed class ToolCreatorResources : ScriptableObject
    {
        public Mesh facetedOrb;
        public Mesh diamond;
        public Mesh wedge;
        public Mesh box;
        public Material solidMaterial;
        public Material hiddenMaterial;
    }
}
