using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = System.Object;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";

        IList<UITypeMapping> m_UITypeMappings;

        public override string ToolName => Name;

        public ShaderGraphStencil() : base()
        {
            InstantiateRegistry();

            CreateRegistryTypeMapping(RegistryInstance.BrowseRegistryKeys());
        }

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) => new SGBlackboardGraphModel(graphAssetModel);

        void CreateRegistryTypeMapping(IEnumerable<RegistryKey> registryKeys)
        {
            m_UITypeMappings = new List<UITypeMapping>();
            m_UITypeMappings.Add(new BoolTypeMapping<Registry.Exploration.GraphTypeDefinition, BooleanConstant>(RegistryInstance));
            m_UITypeMappings.Add(new FloatTypeMapping<Registry.Exploration.GraphTypeDefinition, FloatConstant>(RegistryInstance));
            //m_UITypeMappings.Add(new Vector2TypeMapping<Registry.Exploration.GraphTypeDefinition, Vector2Constant>());
            //m_UITypeMappings.Add(new Vector3TypeMapping<Registry.Exploration.GraphTypeDefinition, Vector3Constant>());
            //m_UITypeMappings.Add(new Vector4TypeMapping<Registry.Exploration.GraphTypeDefinition, Vector4Constant>());
            //m_UITypeMappings.Add(new DynamicTypeMapping<Registry.Exploration.GraphTypeDefinition, ShaderGraphTypes.NumericConstant>());
        }

        public TypeHandle GetTypeHandleFromKey(RegistryKey registryKey)
        {
            foreach (var typeMapping in m_UITypeMappings)
            {
                var mappedRegistryKey = typeMapping.ResolveTypeMapping();
                if (mappedRegistryKey.Equals(registryKey))
                {
                    return typeMapping.GTFType;
                }
            }

            return TypeHandle.Unknown;
        }

        public RegistryKey GetKeyFromTypeHandle(TypeHandle typeHandle)
        {
            foreach (var typeMapping in m_UITypeMappings)
            {
                if (typeMapping.GTFType == typeHandle)
                {
                    return typeMapping.ResolveTypeMapping();
                }
            }

            return new RegistryKey();
        }

        public Type GetConstantNodeType(TypeHandle typeHandle)
        {
            foreach (var typeMapping in m_UITypeMappings)
            {
                if (typeMapping.GTFType == typeHandle || typeMapping.GTFType.Equals(typeHandle))
                {
                    return typeMapping.ConstantType;
                }
            }

            return null;
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            // There's two kinds of types here
            // Deferred/dynamic types (GraphTypes)
            // Static types (Textures/Strings etc.)
            // Need to account for both

            var baseGTFType = TypeToConstantMapper.GetConstantNodeType(typeHandle);
            if (baseGTFType != null)
                return baseGTFType;

            return GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return new ShaderGraphSearcherDatabaseProvider(this);
        }

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return new ShaderGraphSearcherFilterProvider();
        }

        private Registry.Registry RegistryInstance = null;
        public Registry.Registry GetRegistry()
        {
            if (RegistryInstance == null)
            {
                InstantiateRegistry();
            }

            return RegistryInstance;
        }

        void InstantiateRegistry()
        {
            RegistryInstance = new Registry.Registry();
            RegistryInstance.RegisterNodeBuilder<Registry.Exploration.GraphTypeDefinition>();
            RegistryInstance.RegisterNodeBuilder<Registry.Exploration.AddDefinition>();
        }

        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            foreach (var typeMapping in m_UITypeMappings)
            {
                var typeHandle = typeMapping.GTFType;
                menu.AddItem(new GUIContent("Create " + typeHandle.Name), false, () =>
                {
                    const string newItemName = "variable";
                    var finalName = newItemName;
                    var i = 0;
                    // ReSharper disable once AccessToModifiedClosure
                    while (commandDispatcher.State.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                        finalName = newItemName + i++;

                    commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, typeHandle));
                });
            }
        }
    }
}
