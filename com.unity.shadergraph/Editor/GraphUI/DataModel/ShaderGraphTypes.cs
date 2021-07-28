using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Registry.Example;
using UnityEditor.ShaderGraph.Registry.Experimental;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public static class ShaderGraphTypes
    {
        public class NumericConstant : Constant<GraphType>
        {
            public GraphType.Primitive PrimitiveType;
            public GraphType.Precision PrecisionType;

            public static readonly List<string> PrimitiveTypes = new (Enum.GetNames(typeof(GraphType.Primitive)));
            public static readonly List<string> PrecisionTypes = new(Enum.GetNames(typeof(GraphType.Precision)));
        }
    }
}
