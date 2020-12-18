using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Archi.BT.Visualizer
{

    public class BehaviorTreeGraphView : GraphView
    {
        public readonly Vector2 c_NodeSize = new Vector2(100, 150);
        private Color white = new Color32(255, 255, 255, 255);

        private List<Edge> Edges => edges.ToList();
        private List<BTGNodeData> Nodes => nodes.ToList().OfType<BTGNodeData>().Cast<BTGNodeData>().ToList();
        private List<BTGStackNodeData> StackNodes => nodes.ToList().OfType<BTGStackNodeData>().Cast<BTGStackNodeData>().ToList();

        public struct NodePositionInfo
        {
            public int totalChildren;
            public int childIndex;
            public int columnIndex;
            public Vector2 lastNodePosition;
            public Port connectionPort;
        }

        public class FullNodeInfo
        {
            public NodeBase RuntimeNode;
            public NodeProperty PropertyData;
        }

        public BehaviorTreeGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(BehaviorTreeEditorWindow.c_StylePath));
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if(evt.target is BTGNodeData)
            {
                BTGNodeData nodeData = (evt.target as BTGNodeData);
                int itemCount = 1;

                if(nodeData.DecoratorData != null)
                {
                    for(int i = 0; i < nodeData.DecoratorData.Count; i++)
                    {
                        var name = nodeData.DecoratorData[0].RuntimeNode.GetType().Name;
                        evt.menu.InsertAction(0, $"Open {name}", (e) => { OpenFile($"{name}.cs"); });
                        itemCount++;
                    }
                }

                string nodeName = nodeData.MainNodeDetails.RuntimeNode.GetType().Name;
                evt.menu.InsertAction(0, $"Open {nodeName}", (e) => { OpenFile($"{nodeName}.cs"); });

                evt.menu.InsertSeparator("", itemCount);
            }
        }

        private void OpenFile(string className)
        {
            string[] res = Directory.GetFiles(Application.dataPath, className, SearchOption.AllDirectories);

            if(res.Length == 0)
            {
                Debug.LogError($"Unable to locate script {className}");
                return;
            }

            string path = res[0].Replace("\\", "/");

            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1, 0);
        }

        public void ClearTree()
        {
            Nodes.ForEach(x => x.parent.Remove(x));
            StackNodes.ForEach(x => x.parent.Remove(x));
            Edges.ForEach(x => x.parent.Remove(x));
        }

        public void LoadBehaviorTree(NodeBase tree)
        {
            if(tree != null)
            {
                DrawNodes(true, tree, 0, null, null);
                CalculateStackPositions();
            }
        }

        private Image CreateImage(Sprite imageIcon)
        {
            Image icon = new Image()
            {
                style =
                {
                    width = 25,
                    height = 25,
                    marginRight = 5,
                    marginTop = 5,
                    marginLeft = 5
                }
            };

            icon.tintColor = white;

            if (imageIcon != null)
                icon.image = imageIcon.texture;

            return icon;
        }

        public void DrawNodes(bool entryPoint, NodeBase currentNode, int columnIndex, Port parentPort,
                              StackNode stackNode, string[] styleClasses = null, List<FullNodeInfo> decoratorData = null)
        {
            int colIndex = columnIndex;

            FullNodeInfo fullDetails = new FullNodeInfo();
            fullDetails.RuntimeNode = currentNode;

            if (BehaviorTreeEditorWindow.SettingsData == null)
                BehaviorTreeEditorWindow.SettingsData = new DataManager();

            fullDetails.PropertyData = BehaviorTreeEditorWindow.SettingsData.GetNodeStyleDetails(currentNode);

            if (fullDetails.PropertyData != null && fullDetails.PropertyData.IsDecorator)
            {
                if (decoratorData == null)
                    decoratorData = new List<FullNodeInfo>();

                decoratorData.Add(fullDetails);

                if (currentNode.ChildNodes.Count == 0)
                    Debug.Log($"Decorator {currentNode.GetType().Name} doesn't have any children. Nothing will be drawn");
                else
                    DrawNodes(false, currentNode.ChildNodes[0], colIndex, parentPort, stackNode, null, decoratorData);
            }
            else
            {
                BTGNodeData node = new BTGNodeData(fullDetails, entryPoint, parentPort, decoratorData);
                Image nodeIcon = CreateImage(fullDetails.PropertyData.Icon);
                node.titleContainer.Add(nodeIcon);
                nodeIcon.SendToBack();

                VisualElement titleLabel = node.Q<VisualElement>("title-label");
                titleLabel.style.color = new StyleColor(white);
                titleLabel.style.flexGrow = 1;
                titleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

                if (!entryPoint)
                {
                    node.AddPort(GeneratePort(node, Direction.Input, Port.Capacity.Multi), "Parent", true);
                    node.GenerateEdge();

                    if(stackNode != null)
                    {
                        stackNode.AddElement(node);
                        ((BTGStackNodeData)stackNode).childNodes.Add(node);
                    }
                    else
                    {
                        AddElement(node);
                    }

                    if(styleClasses != null)
                    {
                        foreach (string style in styleClasses)
                            node.AddToClassList(style);
                    }
                }
                else
                {
                    AddElement(node);
                }

                if (currentNode.ChildNodes.Count > 0)
                {
                    colIndex++;

                    BTGStackNodeData stack = StackNodes.FirstOrDefault(x => x.ColumnId == colIndex);

                    if (stack == null)
                    {
                        stack = new BTGStackNodeData()
                        {
                            ColumnId = colIndex,
                            style =
                            {
                                width = 350
                            }
                        };

                        Vector2 pos = (Vector2.right * 300) * colIndex;
                        stack.SetPosition(new Rect(pos, c_NodeSize));
                        stack.RemoveFromClassList("stack-node");
                        AddElement(stack);
                    }

                    for(int i = 0; i < currentNode.ChildNodes.Count; i++)
                    {
                        node.AddPort(GeneratePort(node, Direction.Output, Port.Capacity.Multi), (i + 1).ToString(), false);

                        List<string> newStyles = new List<string>();

                        if (i == 0)
                            newStyles.Add("FirstNodeSpacing");
                        else if (i == currentNode.ChildNodes.Count - 1)
                            newStyles.Add("LastNodeSpacing");

                        DrawNodes(false, currentNode.ChildNodes[i], colIndex, node.OutputPorts[i], stack, newStyles.ToArray());
                    }
                }
            }
        }

        public async void CalculateStackPositions()
        {
            await Task.Delay(50);

            Vector2 lastNodePos = Vector2.zero;
            float previousStackNodeHeight = 0;

            for(int i = 0; i < StackNodes.Count; i++)
            {
                Rect originalInfo = StackNodes[i].GetPosition();

                if(i == 0)
                {
                    Rect sizeInfo = Nodes.FirstOrDefault(x => x.EntryPoint).GetPosition();

                    lastNodePos = sizeInfo.center + (Vector2.down * originalInfo.height / 2) + (Vector2.right * (originalInfo.width + 75));
                    StackNodes[i].SetPosition(new Rect(lastNodePos, originalInfo.size));

                    previousStackNodeHeight = sizeInfo.height;
                }
                else
                {
                    float sizeDifference = previousStackNodeHeight - StackNodes[i].GetPosition().height;

                    if (sizeDifference > 100 || sizeDifference < -100)
                    {
                        BTGNodeData[] nodes = StackNodes[i].childNodes.FindAll(x => x.ClassListContains("FirstNodeSpacing") ||
                                                                               x.ClassListContains("LastNodeSpacing")).ToArray();

                        foreach(BTGNodeData nodeData in nodes)
                        {
                            if (nodeData.ClassListContains("FirstNodeSpacing"))
                                nodeData.style.paddingTop = 25;
                            else
                                nodeData.style.paddingBottom = 25;
                        }

                        originalInfo = StackNodes[i].GetPosition();
                    }

                    Rect sizeInfo = StackNodes[i - 1].GetPosition();
                    Vector2 center = lastNodePos + (Vector2.up * sizeInfo.height / 2);

                    lastNodePos = center + (Vector2.down * originalInfo.height / 2) + (Vector2.right * (originalInfo.width + 125));
                    StackNodes[i].SetPosition(new Rect(lastNodePos, originalInfo.size));
                    previousStackNodeHeight = sizeInfo.height;
                }
            }
        }

        private Port GeneratePort(BTGNodeData targetNode, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return targetNode.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(bool));
        }
    }

}
