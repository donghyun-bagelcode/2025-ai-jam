#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

using Newtonsoft.Json;

namespace Dunward.Capricorn
{
    public class GraphView : UnityEditor.Experimental.GraphView.GraphView
    {
        private NodeSearchWindow nodeSearchWindow;
        private Label titleLabel;

        private InputNode inputNode; // This node is the start point of the graph. It is not deletable and unique.
        private ConnectorNode debugStartNode; // This node is the start point of the debug.
        private int lastNodeID = 0;

        private string filePath = null;
        private Stack<string> undoStack = new Stack<string>();

        private JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        public System.Action<string> onChangeFilePath;

        public GraphView(string filePath = null)
        {
            titleLabel = new Label(string.IsNullOrEmpty(filePath) ? "New Graph" : System.IO.Path.GetFileName(filePath));
            titleLabel.AddToClassList("capricorn-graph-title");
            contentContainer.Add(titleLabel);

            if (nodeSearchWindow == null)
                nodeSearchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();

            nodeSearchWindow.onSelectNode = (type, position) => 
            {
                var localPosition = contentViewContainer.WorldToLocal(position);
                var node = (BaseNode)System.Activator.CreateInstance(type, this, ++lastNodeID, localPosition);
                Record();
                AddElement(node);
            };

            if (string.IsNullOrEmpty(filePath))
            {
                inputNode = new InputNode(this, -1, 0, 0);
                AddElement(inputNode);
            }
            else
            {
                Load(filePath);
            }

            UnityEditor.Compilation.CompilationPipeline.compilationStarted += (_) => 
            {
                if (nodes.Count() > 1 || !string.IsNullOrEmpty(filePath)) Save();
            };
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    if (nodes.Count() > 1 || !string.IsNullOrEmpty(filePath)) Save();
                }
            };
            
            serializeGraphElements = (elements) =>
            {
                var datas = new List<NodeMainData>();

                foreach (var element in elements)
                {
                    switch (element)
                    {
                        case ConnectorNode:
                        case OutputNode:
                            var temp = ((BaseNode)element).GetMainData();
                            temp.x += 100;
                            temp.y += 100;
                            datas.Add(temp);
                            break;
                    }
                }

                return JsonConvert.SerializeObject(datas, settings);
            };

            unserializeAndPaste = (operationName, data) =>
            {
                var datas = JsonConvert.DeserializeObject<List<NodeMainData>>(data, settings);

                ClearSelection();
                Record();

                foreach (var nodeData in datas)
                {
                    nodeData.id = ++lastNodeID;

                    switch (nodeData.nodeType)
                    {
                        case NodeType.Connector:
                            var connector = new ConnectorNode(this, nodeData);
                            AddToSelection(connector);
                            AddElement(connector);
                            break;
                        case NodeType.Output:
                            var output = new OutputNode(this, nodeData);
                            AddToSelection(output);
                            AddElement(output);
                            break;
                    }
                }
            };

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(new DragAndDropManipulator(data => Load(data)));
            this.AddManipulator(new ContextualMenuManipulator(evt => evt.menu.InsertAction(0, "Add Node",
                                    (action) => SearchWindow.Open(new SearchWindowContext(action.eventInfo.mousePosition), nodeSearchWindow),
                                    DropdownMenuAction.AlwaysEnabled)));

            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                switch (evt.target)
                {
                    case ConnectorNode connector:
                        evt.menu.InsertAction(0, "Set Debug Start Node", (action) => 
                        { 
                            debugStartNode?.Q(className: "capricorn-debug-start-node")?.RemoveFromHierarchy();
                            var label = new Label("Debug");
                            label.AddToClassList("capricorn-debug-start-node");
                            connector.Add(label);
                            debugStartNode = connector; 
                        });
                        break;
                }
            }));
            
            this.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && evt.actionKey)
                {
                    Save();
                    evt.StopPropagation();
                }

                if (evt.keyCode == KeyCode.Z && evt.actionKey)
                {
                    if (undoStack.Count > 0)
                    {
                        var json = undoStack.Pop();
                        Deserialize(json, false);
                        evt.StopPropagation();
                    }
                }

                if (evt.keyCode == KeyCode.Alpha1 && evt.actionKey)
                {
                    var node = new ConnectorNode(this, ++lastNodeID, GetViewportCenter());
                    Record();
                    AddElement(node);
                    evt.StopPropagation();
                }

                if (evt.keyCode == KeyCode.Alpha2 && evt.actionKey)
                {
                    var node = new OutputNode(this, ++lastNodeID, GetViewportCenter());
                    Record();
                    AddElement(node);
                    evt.StopPropagation();
                }
            });
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public void Record()
        {
            undoStack.Push(SerializeGraph());
        }

        public string SerializeGraph()
        {
            var data = new GraphData();
            data.position = viewTransform.position;
            data.zoomFactor = viewTransform.scale.x;
            data.debugNodeIndex = debugStartNode?.ID ?? -1;

            foreach (var node in nodes)
            {
                if (node is BaseNode capricornGraphNode)
                {
                    data.nodes.Add(capricornGraphNode.GetMainData());
                }
            }

            return JsonConvert.SerializeObject(data, settings);
        }

        public string SerializeSelectionGraph()
        {
            var data = new GraphData
            {
                position = viewTransform.position,
                zoomFactor = viewTransform.scale.x,
                debugNodeIndex = debugStartNode?.ID ?? -1
            };

            foreach (var node in selection)
            {
                if (node is BaseNode capricornGraphNode)
                {
                    data.nodes.Add(capricornGraphNode.GetMainData());
                }
            }
            
            return JsonConvert.SerializeObject(data, settings);;
        }

        public void ClearGraphView()
        {
            ClearGraph();
            inputNode = new InputNode(this, -1, 0, 0);
            AddElement(inputNode);
            filePath = null;
            onChangeFilePath?.Invoke(null);
            UpdateTitleLabel();
        }

        public void Load(string path, bool isTransformChange = true)
        {
            undoStack.Clear();

            filePath = path;
            UpdateTitleLabel();
            onChangeFilePath?.Invoke(path);

            var json = System.IO.File.ReadAllText(path);
            Deserialize(json, isTransformChange);
        }

        public void Deserialize(string json, bool isTransformChange)
        {
            ClearGraph();

            var data = JsonConvert.DeserializeObject<GraphData>(json, settings);

            if (isTransformChange)
            {
                viewTransform.position = data.position;
                viewTransform.scale = new Vector3(data.zoomFactor, data.zoomFactor, 1);
            }

            foreach (var nodeData in data.nodes)
            {
                switch (nodeData.nodeType)
                {
                    case NodeType.Input:
                        inputNode = new InputNode(this, nodeData);
                        AddElement(inputNode);
                        break;
                    case NodeType.Output:
                        var outputNode = new OutputNode(this, nodeData);
                        AddElement(outputNode);
                        break;
                    case NodeType.Connector:
                    default:
                        var node = new ConnectorNode(this, nodeData);
                        AddElement(node);
                        break;
                }
            }

            ConnectDeserializeNodes();
            lastNodeID = data.nodes.Max(n => n.id);

            var debugNode = nodes.FirstOrDefault(n => n is ConnectorNode && ((BaseNode)n).ID == data.debugNodeIndex) as ConnectorNode;
            if (debugNode != null)
            {
                debugStartNode = debugNode;
                debugNode.Q(className: "capricorn-debug-start-node")?.RemoveFromHierarchy();
                var label = new Label("Debug");
                label.AddToClassList("capricorn-debug-start-node");
                debugNode.Add(label);
            }
        }
     
        public void Save()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                SaveAs();
                return;
            }

            var json = SerializeGraph();
            System.IO.File.WriteAllText(filePath, json);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        public void SaveAs()
        {
            // editor directiory
            var path = EditorUtility.SaveFilePanel("Save Graph", Application.dataPath, null, "json");
            if (string.IsNullOrEmpty(path)) return;

            filePath = path;
            UpdateTitleLabel();
            onChangeFilePath?.Invoke(path);
            Save();
        }

        public string ExportOnlyActionToCSV()
        {
            var csv = "";

            foreach (var node in nodes)
            {
                if (node is BaseNode baseNode)
                {
                    var unit = baseNode.main.action.data.action;
                    int type = 0;

                    if (unit is TextTypingUnit) type = 1;
                    if (unit is SelectionUnit) type = 2;

                    if (type == 0) continue;

                    var scripts = "";
                    
                    if (unit is TextTypingUnit textTypingUnit)
                    {
                        scripts = $"{textTypingUnit.name},{textTypingUnit.subName},{textTypingUnit.script.Replace(System.Environment.NewLine, "\\n")}";
                    }

                    if (unit is SelectionUnit selectionUnit)
                    {
                        scripts = string.Join(",", selectionUnit.scripts);
                    }

                    csv += $"{baseNode.ID},{type},{scripts}\n";
                }
            }

            return csv;
        }

        public void ImportOnlyActionFromCSV(string path)
        {
            var csv = System.IO.File.ReadAllLines(path);

            foreach (var line in csv)
            {
                var data = line.Split(',');
                var id = int.Parse(data[0]);
                var type = int.Parse(data[1]);

                var node = nodes.FirstOrDefault(n => n is BaseNode baseNode && baseNode.ID == id) as BaseNode;
                if (type == 1)
                {
                    var unit = node.main.action.data.action as TextTypingUnit;
                    unit.name = data[2];
                    unit.subName = data[3];
                    unit.script = string.Join(",", data.Skip(4)).Replace("\\n", System.Environment.NewLine);
                }

                if (type == 2)
                {
                    var unit = node.main.action.data.action as SelectionUnit;
                    unit.scripts = data.Skip(2).ToList();
                }
            }
        }

        public void DeserializeNodesOnly(string json)
        {
            var data = JsonConvert.DeserializeObject<GraphData>(json, settings);

            List<BaseNode> nodes = new List<BaseNode>();
            foreach (var nodeData in data.nodes)
            {
                BaseNode node = null;
                switch (nodeData.nodeType)
                {
                    case NodeType.Input:
                        node = new InputNode(this, nodeData);
                        AddElement(node);
                        break;
                    case NodeType.Output:
                        node = new OutputNode(this, nodeData);
                        AddElement(node);
                        break;
                    case NodeType.Connector:
                    default:
                        node = new ConnectorNode(this, nodeData);
                        AddElement(node);
                        break;
                }
                nodes.Add(node);
            }

            ConnectDeserializeNodes(nodes);
            lastNodeID = data.nodes.Max(n => n.id);
        }

        public void ReplaceNodesFromJson(string json)
        {
            var data = JsonConvert.DeserializeObject<GraphData>(json, settings);

            foreach (var node in data.nodes)
            {
                Debug.Log(node.id);
                // replace nodemaindata matching with node id
                var targetNode = nodes.FirstOrDefault(n => n is BaseNode baseNode && baseNode.ID == node.id) as BaseNode;
                
                targetNode.main.action.DeserializeActions(node.actionData);
                targetNode.main.coroutine.DeserializeCoroutines(node.coroutineData);
            }
        }

        public (Vector2, float, int) GetGraphViewData()
        {
            return (viewTransform.position, viewTransform.scale.x, lastNodeID);
        }

        private void ConnectDeserializeNodes(List<BaseNode> nodes)
        {
            foreach (var node in nodes)
            {
                var baseNode = node;
                var nodeActionData = baseNode.main.action.data;
                for (int i = 0; i < nodeActionData.action.connections.Count; i++)
                {
                    var connection = nodeActionData.action.connections[i];
                    var outputPort = baseNode.outputContainer[i] as Port;
                    var inputPort = nodes.Where(n => n is BaseNode)
                                        .Cast<BaseNode>()
                                        .FirstOrDefault(n => n.ID == connection)?.inputContainer[0] as Port;

                    if (inputPort == null) continue;

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        private void ConnectDeserializeNodes()
        {
            foreach (var node in nodes)
            {
                var baseNode = node as BaseNode;
                for (int i = 0; i < baseNode.main.action.data.action.connections?.Count; i++)
                {
                    var connection = baseNode.main.action.data.action.connections[i];
                    var outputPort = baseNode.outputContainer[i] as Port;
                    var inputPort = nodes.Where(n => n is BaseNode)
                                        .Cast<BaseNode>()
                                        .FirstOrDefault(n => n.ID == connection)?.inputContainer[0] as Port;

                    if (inputPort == null) continue;

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        private void ClearGraph()
        {
            DeleteElements(nodes.ToList());
            DeleteElements(edges.ToList());
        }

        private void UpdateTitleLabel()
        {
            titleLabel.text = string.IsNullOrEmpty(filePath) ? "New Graph" : System.IO.Path.GetFileName(filePath);
        }

        private Vector3 GetViewportCenter()
        {
            var viewport = contentViewContainer.parent;
            var viewportBounds = viewport.worldBound;

            var viewTransformMatrix = viewTransform.matrix;

            var viewportCenter = new Vector2(
                viewportBounds.xMin + viewportBounds.width / 2f,
                viewportBounds.yMin + viewportBounds.height / 2f
            );

            var contentCenter = viewTransformMatrix.inverse.MultiplyPoint3x4(viewportCenter);

            return contentCenter;
        }

        private class DragAndDropManipulator : PointerManipulator
        {
            private Object item;
            private readonly System.Action<string> onLoadGraph;

            public DragAndDropManipulator(System.Action<string> onLoadGraph)
            {
                this.onLoadGraph = onLoadGraph;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<DragEnterEvent>(OnDragEnter);
                target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
                target.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
                target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
                target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
                target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            }

            private void OnDragEnter(DragEnterEvent _)
            {
                item = DragAndDrop.objectReferences[0];
            }

            private void OnDragLeave(DragLeaveEvent _)
            {
                item = null;
            }

            private void OnDragUpdated(DragUpdatedEvent _)
            {
                DragAndDrop.visualMode = IsSingleTextAsset() ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            }

            private void OnDragPerform(DragPerformEvent _)
            {
                EditorUtility.DisplayProgressBar("Capricorn", "Load Graph...", 0.112f);
                var textAsset = item as TextAsset;
                var path = AssetDatabase.GetAssetPath(textAsset);
                onLoadGraph?.Invoke(path);
                EditorUtility.ClearProgressBar();
            }

            private bool IsSingleTextAsset()
            {
                return item is TextAsset && DragAndDrop.objectReferences.Length == 1;
            }
        }
    }
}
#endif