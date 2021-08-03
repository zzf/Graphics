using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon
{
    public interface IPropertyDefinitionBuilder { }

    // Need to know if we map to a property with a material property block underlying it
    // Needs a field on the type mapping that indicates that (i.e. the type of the MPB) to understand the fields that actually exist on it
    // This because that property requires some more information at the time of initialization that we will need to provide (for instance Vec3 vs Vec2s at a certain precision)
    public abstract class UITypeMapping
    {
        public virtual Type DataType { get; }
        public virtual Type ConstantType { get; }
        // Meant to overriden with TypeHandle.Bool/Float etc.
        public virtual TypeHandle GTFType { get; }

        protected Registry.Registry registryInstance { get; }

        protected UITypeMapping(Registry.Registry registryInstance)
        {
            this.registryInstance = registryInstance;
        }

        public abstract RegistryKey ResolveTypeMapping();
    }

    public class BoolTypeMapping<GraphType, ConstantDataType> : UITypeMapping where GraphType : INodeDefinitionBuilder
    {
        public override Type DataType => typeof(GraphType);
        public override Type ConstantType => typeof(ConstantDataType);
        public override TypeHandle GTFType => TypeHandle.Bool;

        public override RegistryKey ResolveTypeMapping()
        {
            return registryInstance.ResolveKey<GraphType>();
        }

        public BoolTypeMapping(Registry.Registry registryInstance)
            : base(registryInstance) { }
    }

    public class FloatTypeMapping<GraphType, ConstantDataType> : UITypeMapping where GraphType : INodeDefinitionBuilder
    {
        public override Type DataType => typeof(GraphType);
        public override Type ConstantType => typeof(ConstantDataType);
        public override TypeHandle GTFType => TypeHandle.Float;

        public override RegistryKey ResolveTypeMapping()
        {
            return registryInstance.ResolveKey<GraphType>();
        }

        public FloatTypeMapping(Registry.Registry registryInstance)
            : base(registryInstance) { }
    }

    /*public class Vector2TypeMapping<GraphType, Vector2Constant> : UITypeMapping
    {
        public Type DataType => typeof(Registry.Exploration.GraphTypeDefinition);
        public Type ConstantType => typeof(Vector2Constant);
        public TypeHandle GTFType => TypeHandle.Vector2;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.ResolveKey<Registry.Exploration.GraphTypeDefinition>();
        }
    }

    public class Vector3TypeMapping<GraphType, Vector3Constant> : UITypeMapping
    {
        public Type DataType => typeof(Registry.Exploration.GraphTypeDefinition);
        public Type ConstantType => typeof(Vector3Constant);
        public TypeHandle GTFType => TypeHandle.Vector3;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.ResolveKey<Registry.Exploration.GraphTypeDefinition>();
        }
    }

    public class Vector4TypeMapping<GraphType, Vector4Constant> : UITypeMapping
    {
        public Type DataType => typeof(Registry.Exploration.GraphTypeDefinition);
        public Type ConstantType => typeof(Vector4Constant);
        public TypeHandle GTFType => TypeHandle.Vector4;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.ResolveKey<Registry.Exploration.GraphTypeDefinition>();
        }
    }

    public class DynamicTypeMapping<GraphType, NumericConstant> : UITypeMapping
    {
        public Type DataType => typeof(Registry.Exploration.GraphTypeDefinition);
        public Type ConstantType => typeof(ShaderGraphTypes.NumericConstant);
        public TypeHandle GTFType => TypeHandleHelpers.GenerateTypeHandle<ShaderGraphTypes.NumericConstant>();
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.ResolveKey<Registry.Exploration.GraphTypeDefinition>();
        }
    }

    public class StringTypeMapping<StringLiteralNode, NumericConstant> : UITypeMapping
    {
        public Type DataType => typeof(Registry.Example.StringLiteralNode);
        public Type ConstantType => typeof(StringConstant);
        public TypeHandle GTFType => TypeHandle.String;
        public RegistryKey ResolveTypeMapping()
        {
            return Registry.ResolveKey<Registry.Example.StringLiteralNode>();
        }
    }*/

}
