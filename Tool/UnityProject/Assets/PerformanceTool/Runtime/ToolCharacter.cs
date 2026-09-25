using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceTool
{
    [CreateAssetMenu(menuName = "Astra/Performance Tool/Character")]
    public sealed class ToolCharacter : ScriptableObject
    {
        public string id = "lin";
        public string displayName = "林";
        public ToolSpecies species;
        public GameObject sourceModel;
        public Material[] v3Materials = Array.Empty<Material>();
        public Material accessoryMaterial;
        public ToolCharacterLook look = new ToolCharacterLook();
    }
}
