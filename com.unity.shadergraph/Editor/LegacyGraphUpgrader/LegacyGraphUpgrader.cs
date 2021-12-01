using System.Collections.Generic;
using System.Linq;



namespace UnityEditor.ShaderGraph.LegacyGraphUpgrader
{
    // example upgrader...
    class AddNodeUpgrader : INodeUpgrader
    {
        public void Register(INodeUpgraderRegistration register)
        {
            // can create more complex upgraders that handle multiple node types
            register.HandleNodeTypes(typeof(UnityEditor.ShaderGraph.AddNode));
        }

        public void UpgradeNode(AbstractMaterialNode sourceNode, INodeUpgraderContext context)
        {
            // upgrader can create multiple nodes if needed (if nodes split on upgrade)
            // upgrader does not have to handle creating nodes for slot defaults -- that is done automatically
            var destNode = context.CreateNode(AddNode.StaticRegistryKey);
            context.MapSlot(0, destNode, "In0");
            context.MapSlot(1, destNode, "In1");
            context.MapSlot(2, destNode, "Out");
        }
    }

    public interface INodeUpgrader
    {
        void Register(INodeUpgraderRegistration register);

        // create some set of dest nodes that are equivalent to the source node
        // * setup node internal state appropriately
        // * connect the dest nodes appropriately to each other
        // * provide mappings from source node slot => dest node slot
        //   (this is used to upgrade existing connections and inline slot values)
        void UpgradeNode(AbstractMaterialNode sourceNode, INodeUpgraderContext context);
    }

    public interface INodeUpgraderRegistration
    {
        void HandleNodeTypes(params Type[] nodeTypes);
    }

    public interface INodeUpgraderContext
    {
        // create a node
        // the upgrader can create multiple nodes if needed
        // the upgrader should create any internal connections between these nodes
        // and also provide slot mappings from the old slots to the new ports on those nodes
        // the node upgrader does not handle upgrading old slots or connections between old slots
        // that will be done automatically based on the provided slot mappings
        INodeWriter CreateNode(RegistryKey registerKey, string id = null);

        // override a slot mapping (if something special needs to happen)
        // if not provided, the default mapping will be assumed
        // (connects to equivalent port on the first created node)
        void MapSlot(int slotId, INodeWriter destNode, string portKey);
        void MapSlot(MaterialSlot sourceSlot, INodeWriter destNode, string portKey);
        void MapSlot(SlotReference sourceSlot, INodeWriter destNode, string portKey);
    }

    class NodeUpgraderContext : INodeUpgraderContext
    {
        IGraphHandler destGraph;
        Registry.Registry registry;

        // map from old slots to new ports (only when overrides are provided)
        Dictionary<SlotReference, PortReference> slotMap;

        // map from old nodes to new nodes (only the first created new node)
        Dictionary<AbstractMaterialNode, INodeWriter> nodeMap;

        // current node we are upgrading
        AbstractMaterialNode sourceNode;
        int destNodesCreated;

        internal void SetupForGraph(
            IGraphHandler destGraph,
            Registry.Registry registry,
            Dictionary<SlotReference, PortReference> slotMap,
            Dictionary<AbstractMaterialNode, INodeWriter> nodeMap)
        {
            this.destGraph = destGraph;
            this.registry = registry;
            this.slotMap = slotMap;
            this.nodeMap = nodeMap;
        }

        internal void SetupForNode(AbstractMaterialNode sourceNode)
        {
            this.sourceNode = sourceNode;
            this.destNodesCreated = 0;
        }

        public INodeWriter CreateNode(RegistryKey registryKey, string id = null)
        {
            if (id == null)
            {
                // TODO: do we want anything special here translating the node ids?
                id = $"{sourceNode.objectId}_{destNodesCreated}";
            }
            else
            {
                // TODO: should probably at least ensure the requested id is unique here...
            }

            var destNode = destGraph.AddNode(registryKey, id, registry);
            if (destNodesCreated == 0)
            {
                // capture first created node as the default node map
                nodeMap.Add(sourceNode, destNode);
            }
            destNodesCreated++;
            return destNode;
        }

        public void MapSlot(int slotId, INodeWriter destNode, string portKey)
        {
            MapSlot(new SlotReference(sourceNode, slotId), destNode, portKey);
        }

        public void MapSlot(MaterialSlot sourceSlot, INodeWriter destNode, string portKey)
        {
            MapSlot(sourceSlot.slotReference, destNode, portKey);
        }

        public void MapSlot(SlotReference sourceSlot, INodeWriter destNode, string portKey)
        {
            slotMap.Add(sourceSlot, new PortReference(destNode, portKey));
        }
    }

    readonly struct PortReference
    {
        public readonly INodeWriter destNode;
        public readonly string portKey;

        public PortReference(INodeWriter destNode, string portKey)
        {
            this.destNode = destNode;
            this.portKey = portKey;
        }

        public bool isValid => (destNode != null) && (portKey != null);
    };


    class LegacyGraphUpgrader : INodeUpgraderRegistration
    {
        NodeUpgraderContext nodeUpgraderContext;

        GraphData sourceGraph;
        IGraphHandler destGraph;
        Registry.Registry registry;

        // upgraders for each node type
        // (can be static as it only needs to be rebuilt when assemblies reload)
        static Dictionary<Type, INodeUpgrader> nodeUpgraders;

        // node map
        Dictionary<AbstractMaterialNode, INodeWriter> nodeMap = new Dictionary<AbstractMaterialNode, INodeWriter>();

        // slot map
        Dictionary<SlotReference, PortReference> slotMap = new Dictionary<SlotReference, PortReference>();

        // property map
        // Dictionary<AbstractShaderProperty, IProperty> propertyMap;

        INodeUpgrader currentUpgrader;
        void INodeUpgraderRegistration.HandleNodeType(Type[] nodeTypes)
        {
            foreach (var nodeType in nodeTypes)
                nodeUpgraders.Add(nodeType, currentUpgrader);
        }

        void BuildNodeUpgraders()
        {
            if (nodeUpgraders == null)
            {
                nodeUpgraders = new Dictionary<Type, INodeUpgrader>();

                // look for all classes implementing INodeUpgrader, instantiate and register them
                var upgraderTypes = TypeCache.GetTypesDerivedFrom<INodeUpgrader>();
                foreach (var upgraderType in upgraderTypes)
                {
                    currentUpgrader = Activator.CreateInstance(upgraderType, true);
                    currentUpgrader?.Register(this);
                }
                currentUpgrader = null;
            }
        }

        void UpgradeProperty(AbstractShaderProperty sourceProperty)
        {
            // TODO (need property representation in dest graph)
            // var propType = sourceProperty.GetType();
            // destGraph.AddProperty(...);
        }

        void UpgradeKeyword(ShaderKeyword sourceKeyword)
        {
            // TODO
        }

        void UpgradeDropdown(ShaderDropdown sourceDropdown)
        {
            // TODO
        }

        void UpgradeCategory(CategoryData sourceCategory)
        {
            // TODO
        }

        void UpgradeGroup(GroupData sourceGroup)
        {
            // TODO
        }

        void UpgradeNote(StickyNoteData sourceNote)
        {
            // TODO
        }

        void UpgradePreviewData(InspectorPreviewData previewData)
        {
            // TODO
        }

        void UpgradeNode(AbstractMaterialNode sourceNode)
        {
            var nodeType = sourceNode.GetType();
            if (nodeUpgraders.TryGetValue(nodeType, out INodeUpgrader nodeUpgrader))
            {
                nodeUpgraderContext.SetupForNode(sourceNode);
                nodeUpgrader.UpgradeNode(sourceNode, nodeUpgraderContext);
            }
        }

        // tries to copy the default value on the sourceSlot to the equivalent slot on the dest node
        bool CopySlotValueToPort(MaterialSlot sourceSlot, PortReference destPort)
        {
            // TODO: need to figure out how inline values assigned to ports is represented in the new graph system
            // here I'm assuming it is a field named "value" attached to the port
            // also this seems like we could add some helpers that make this more clear from the API level

            if (!sourceSlot.isInput)
                return false;

            var inputPortKey = SlotToPortKey(sourceSlot);
            bool isHorizontal = true;   // TODO: what is this?????

            if (!destNode.TryAddPort(inputPortKey, true, isHorizontal, out IPortWriter inputPort))
                return false;

            if (sourceSlot is SamplerStateMaterialSlot ssSlot)
            {
                /*
                // TODO need to figure out what is the data representation of SamplerState...
                if (inputPort.TryAddField<SamplerStateDataType>("value", out var field))
                {
                    return field.TryWriteData(ssSlot.value);
                }
                */
            }
            else if (sourceSlot is DynamicMatrixMaterialSlot dmSlot)
            {
            }
            else if (sourceSlot is Matrix4MaterialSlot m4Slot)
            {
                if (inputPort.TryAddField<Matrix4x4>("value", out var field))
                    return field.TryWriteData(m4Slot.value);
            }
            else if (sourceSlot is Matrix3MaterialSlot m3Slot)
            {
                // is there a Matrix3x3 ?
                if (inputPort.TryAddField<Matrix4x4>("value", out var field))
                    return field.TryWriteData(m3Slot.value);
            }
            else if (sourceSlot is Matrix2MaterialSlot m2Slot)
            {
                // is there a Matrix2x2 ?
                if (inputPort.TryAddField<Matrix4x4>("value", out var field))
                    return field.TryWriteData(m2Slot.value);
            }
            else if (sourceSlot is Texture2DInputMaterialSlot t2Slot)
            {
                // TODO: Texture2DArray type is not serialize-able .. probably should use a SerializableTextureArray instead?
                if (inputPort.TryAddField<Texture2D>("value", out var field))
                    return field.TryWriteData(t2Slot.texture);
            }
            else if (sourceSlot is Texture2DMaterialSlot t2Slot)
            {
                // does not contain default values
            }
            else if (sourceSlot is Texture2DArrayInputMaterialSlot t2aSlot)
            {
                // TODO: Texture2DArray type is not serialize-able .. probably should use a SerializableTextureArray instead?
                if (inputPort.TryAddField<Texture2DArray>("value", out var field))
                    return field.TryWriteData(t2aSlot.textureArray);
            }
            else if (sourceSlot is Texture2DArrayMaterialSlot t2Slot)
            {
                // does not contain default values
            }
            else if (sourceSlot is Texture3DInputMaterialSlot t3Slot)
            {
                // TODO: Texture type is not serialize-able .. probably should use a SerializableTexture instead?
                if (inputPort.TryAddField<Texture>("value", out var field))
                    return field.TryWriteData(t3Slot.texture);
            }
            else if (sourceSlot is Texture3DMaterialSlot t3Slot)
            {
                // does not contain default values
            }
            else if (sourceSlot is CubemapInputMaterialSlot cmSlot)
            {
                // TODO: Cubemap type is not serialize-able .. probably should use a SerializableCubemap instead?
                if (inputPort.TryAddField<Cubemap>("value", out var field))
                    return field.TryWriteData(cmSlot.cubemap);
            }
            else if (sourceSlot is CubemapMaterialSlot cmSlot)
            {
                // does not contain default values
            }
            else if (sourceSlot is VirtualTextureInputMaterialSlot vtSlot)
            {
                // VT slots do not have default values (they can't be inlined)
            }
            else if (sourceSlot is VirtualTextureMaterialSlot vtSlot)
            {
                // does not contain default values
            }
            else if (sourceSlot is GradientInputMaterialSlot grSlot)
            {
                if (inputPort.TryAddField<Gradient>("value", out var field))
                    return field.TryWriteData(grSlot.value);
            }
            else if (sourceSlot is GradientMaterialSlot grSlot)
            {
                // does not contain default values
            }
            else if (sourceSlot is DynamicVectorMaterialSlot dvSlot)
            {

            }
            else if (sourceSlot is Vector4MaterialSlot v4Slot)
            {
                if (inputPort.TryAddField<Vector4>("value", out var field))
                    return field.TryWriteData(v4Slot.value);
            }
            else if (sourceSlot is Vector3MaterialSlot v3Slot)
            {
                if (inputPort.TryAddField<Vector3>("value", out var field))
                    return field.TryWriteData(v3Slot.value);
            }
            else if (sourceSlot is Vector2MaterialSlot v2Slot)
            {
                if (inputPort.TryAddField<Vector2>("value", out var field))
                    return field.TryWriteData(v2Slot.value);
            }
            else if (sourceSlot is Vector1MaterialSlot v1Slot)
            {
                if (inputPort.TryAddField<float>("value", out var field))
                    return field.TryWriteData(v1Slot.value);
            }
            else if (sourceSlot is DynamicValueMaterialSlot dvSlot)
            {

            }
            else if (sourceSlot is BooleanMaterialSlot bSlot)
            {
                if (inputPort.TryAddField<float>("value", out var field))
                    return field.TryWriteData(v1Slot.value);
            }
            else if (sourceSlot is PropertyConnectionStateMaterialSlot pcSlot)
            {

            }

            return false; // failed
        }

        internal static string SlotToPortKey(MaterialSlot slot)
        {
            return SlotToPortKey(slot.slotReference);
        }

        internal static string SlotToPortKey(SlotReference slot)
        {
            // TODO: node upgrader might want to be involved here to map old slot id ==> new port key?
            // any restrictions on port keys?  are they arbitrary strings?  identifiers?  specific format?
            return $"slot{slot.slotId}";
        }

        internal PortReference ConvertSlotToPortReference(SlotReference slot)
        {
            PortReference port;
            if (!slotMap.TryGetValue(slot, out port))
            {
                // no slot mapping provided -- use default mapping via nodes
                if (nodeMap.TryGetValue(slot.node, out INodeWriter destNode))
                {
                    var portKey = SlotToPortKey(slot);
                    port = new PortReference(destNode, portKey);
                }
            }
            return port;
        }

        public IGraphHandler UpgradeGraph(GraphData source, Registry.Registry registry)
        {
            this.sourceGraph = source;
            this.destGraph = GraphUtil.CreateGraph();
            this.registry = registry;
            this.nodeMap.Clear();
            this.slotMap.Clear();
            nodeUpgraderContext = new NodeUpgraderContext();
            nodeUpgraderContext.SetupForGraph(this.destGraph, this.registry, this.nodeMap, this.slotMap);

            BuildNodeUpgraders();

            // Setup graph global data
                // sourceGraph.graphDefaultPrecision
                // sourceGraph.isSubGraph
                // sourceGraph.previewMode
                // sourceGraph.outputNode       // for sub graphs only
                // sourceGraph.path             // ??

            // Copy Targets
            // TODO: unless we use the same json serialization format, we're not going to be able to transfer unknown targets
            // sourceGraph.activeTargets
            // sourceGraph.allPotentialTargets

            // Copy Block Nodes (?)
            // sourceGraph.vertexContext
            // sourceGraph.fragmentContext

            // Copy Properties
            foreach (var sourceProperty in sourceGraph.properties)
                UpgradeProperty(sourceProperty);

            // Copy Keywords
            foreach (var sourceKeyword in sourceGraph.keywords)
                UpgradeKeyword(sourceKeyword);

            // Copy Dropdowns (should be in subgraphs only?)
            foreach (var sourceDropdown in sourceGraph.dropdowns)
                UpgradeDropdown(sourceDropdown);

            // Copy Categories
            foreach (var sourceCategory in sourceGraph.categories)
                UpgradeCategory(sourceCategory);

            // Copy Nodes
            foreach (var sourceNode in sourceGraph.GetNodes<AbstractMaterialNode>())
                UpgradeNode(sourceNode);

            // Copy Notes
            foreach (var sourceNote in sourceGraph.stickyNotes)
                UpgradeNote(sourceNote);

            // Copy Groups
            foreach (var sourceGroup in sourceGraph.groups)
                UpgradeGroup(sourceGroup);

            // Copy Edges
            foreach (var sourceEdge in sourceGraph.edges)
            {
                PortReference inputPort = ConvertSlotToPortReference(sourceEdge.inputSlot);
                PortReference outputPort = ConvertSlotToPortReference(sourceEdge.outputSlot);
                if (inputPort.isValid && outputPort.isValid)
                {
                    bool isHorizontal = true;   // TODO: what is this?????
                    if (inputPort.destNode.TryAddPort(inputPort.portKey, true, isHorizontal, out IPortWriter inputPort) &&
                        outputPort.destNode.TryAddPort(outputPort.portKey, false, isHorizontal, out IPortWriter outputPort))
                    {
                        inputPort.TryAddConnection(outputPort);
                    }
                }
            }

            // Copy defaults from each node's unconnected input slots
            var sourceNodeSlots = new List<MaterialSlot>();
            foreach (var sourceNode in sourceGraph.GetNodes())
            {
                sourceNodeSlots.Clear();
                sourceNode.GetSlots<MaterialSlot>(sourceNodeSlots);
                foreach (MaterialSlot sourceSlot in sourceNodeSlots)
                {
                    if (sourceSlot.isInput && !sourceSlot.isConnected)
                    {
                        PortReference port = ConvertSlotToPortReference(sourceSlot.slotReference);
                        if (port.isValid)
                            CopySlotValueToPort(sourceSlot, port);
                    }
                }
            }

            UpgradePreviewData(sourceGraph.previewData);

            return destGraph;
        }
    }
}
