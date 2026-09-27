using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceTool
{
    [CreateAssetMenu(menuName = "Astra/Performance Tool/Package")]
    public sealed class ToolPackage : ScriptableObject
    {
        public ToolLibrary library;
        public ToolGraph graph;
        public ToolStat[] initialStats = Array.Empty<ToolStat>();
        public int initialPatience = 10;
    }
}
