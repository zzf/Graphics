using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.GraphElements;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.Factory
{
    [GraphElementsExtensionMethodsCache(typeof(ShaderGraphView))]
    public static class ShaderGraphViewFactoryExtensions
    {
        public static IModelUI CreateRegistryNode(this ElementBuilder elementBuilder, CommandDispatcher store, RegistryNodeModel model)
        {
            var ui = new RegistryNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /*public static VisualElement CreateCustomTypeEditor(this IConstantEditorBuilder editorBuilder,
            ShaderGraphTypes.NumericConstant c)
        {
            var primitiveTypeDropdown = new DropdownField(ShaderGraphTypes.NumericConstant.PrimitiveTypes, 0);
            primitiveTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (Enum.TryParse(evt.newValue, true, out Registry.Exploration.GraphTypeDefinition.Primitive equivalentType))
                {
                    c.PrimitiveType = equivalentType;
                }
            });

            var precisionTypeDropdown = new DropdownField(ShaderGraphTypes.NumericConstant.PrecisionTypes, 0);
            precisionTypeDropdown.RegisterValueChangedCallback(evt =>
            {
                if (Enum.TryParse(evt.newValue, true, out Registry.Exploration.GraphTypeDefinition.Precision equivalentType))
                {
                    c.PrecisionType = equivalentType;
                }
            });


            var root = new VisualElement();
            //root.Add(primitiveTypeDropdown);
            root.Add(precisionTypeDropdown);

            return root;
        }*/
    }
}
