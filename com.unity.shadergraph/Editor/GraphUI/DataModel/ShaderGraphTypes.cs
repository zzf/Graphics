using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public static class ShaderGraphTypes
    {
        public class NumericConstant : Constant<Registry.Exploration.GraphTypeDefinition>
        {
            public Registry.Exploration.GraphTypeDefinition.Precision PrimitiveType;
            //public GraphType.Precision PrecisionType;
            //
            //public static readonly List<string> PrimitiveTypes = new (Enum.GetNames(typeof(GraphType.Primitive)));
            //public static readonly List<string> PrecisionTypes = new(Enum.GetNames(typeof(GraphType.Precision)));
        }
    }
}
