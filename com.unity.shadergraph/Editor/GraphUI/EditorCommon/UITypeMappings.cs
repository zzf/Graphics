using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.Registry.Experimental;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon
{
    public interface IPropertyDefinitionBuilder { }

    // Need to know if we map to a property with a material property block underlying it
    // Needs a field on the type mapping that indicates that (i.e. the type of the MPB) to understand the fields that actually exist on it
    // This because that property requires some more information at the time of initialization that we will need to provide (for instance Vec3 vs Vec2s at a certain precision)
    //
    public interface IUITypeMapping
    {
        public Type DataType { get; }
        public Type ConstantType { get; }
        // Meant to overriden with TypeHandle.Bool/Float etc.
        public TypeHandle GTFType { get; }

        public RegistryKey ResolveTypeMapping();
    }

    public class BoolTypeMapping<GraphType, BooleanConstant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(BooleanConstant);
        public TypeHandle GTFType => TypeHandle.Bool;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class FloatTypeMapping<GraphType, FloatConstant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(FloatConstant);
        public TypeHandle GTFType => TypeHandle.Float;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class Vector2TypeMapping<GraphType, Vector2Constant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(Vector2Constant);
        public TypeHandle GTFType => TypeHandle.Vector2;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class Vector3TypeMapping<GraphType, Vector3Constant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(Vector3Constant);
        public TypeHandle GTFType => TypeHandle.Vector3;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class Vector4TypeMapping<GraphType, Vector4Constant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(Vector4Constant);
        public TypeHandle GTFType => TypeHandle.Vector4;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class DynamicTypeMapping<GraphType, NumericConstant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.GraphType);
        public Type ConstantType => typeof(ShaderGraphTypes.NumericConstant);
        public TypeHandle GTFType => TypeHandleHelpers.GenerateTypeHandle<ShaderGraphTypes.NumericConstant>();
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.GraphType>();
        }
    }

    public class StringTypeMapping<StringLiteralNode, NumericConstant> : IUITypeMapping
    {
        public Type DataType => typeof(Registry.Example.StringLiteralNode);
        public Type ConstantType => typeof(StringConstant);
        public TypeHandle GTFType => TypeHandle.String;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.Experimental.Registry.ResolveKey<Registry.Example.StringLiteralNode>();
        }
    }

}
