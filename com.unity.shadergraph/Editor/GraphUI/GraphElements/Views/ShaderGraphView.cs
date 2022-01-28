using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    public class ShaderGraphView : GraphView
    {
        public ShaderGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName)
            : base(window, graphTool, graphViewName)
        {
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            for (var i = 0; i < evt.menu.MenuItems().Count; ++i)
            {
                var menuItem = evt.menu.MenuItems()[i];
                if (menuItem is DropdownMenuAction { name: "Disable Nodes" })
                    evt.menu.RemoveItemAt(i);
            }
            evt.menu.AppendSeparator();
        }

        protected override void CollectCopyableGraphElements(
            IEnumerable<IGraphElementModel> elements,
            HashSet<IGraphElementModel> elementsToCopySet)
        {
            var elementsList = elements.ToList();
            base.CollectCopyableGraphElements(elementsList, elementsToCopySet);

            // Pasting a redirect should also paste an edge to its source node.
            foreach (var redirect in elementsList.OfType<RedirectNodeModel>())
            {
                var incomingEdge = redirect.GetIncomingEdges().FirstOrDefault();
                if (incomingEdge != null) elementsToCopySet.Add(incomingEdge);
            }
        }
    }
}
