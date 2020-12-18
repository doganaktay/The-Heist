using System;
using System.Collections.Generic;
using UnityEngine;
using GraphView = UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using static Archi.BT.Visualizer.BehaviorTreeGraphView;

namespace Archi.BT.Visualizer
{
    public enum DecoratorTypes
    {
        None,
        Inverter,
        Timer,
        UntilFail,
        Repeater
    }

    [Serializable]
    public class BTGNodeData : GraphView.Node
    {
        public string Id { get; private set; }
        public Vector2 Position { get; private set; }
        public bool EntryPoint { get; private set; }
        public FullNodeInfo MainNodeDetails { get; private set; }
        public List<FullNodeInfo> DecoratorData { get; private set; }

        public GraphView.Port ParentPort { get; private set; }
        public GraphView.Port InputPort { get; private set; }
        public List<GraphView.Port> OutputPorts = new List<GraphView.Port>();

        private VisualElement nodeBorder;
        private VisualElement nodeTitleContainer;
        private Label nodeTopMessageGeneral;
        private Label nodeTopMessageDecorator;
        private Label nodeLastEvaluatedTimeStamp;
        private Image statusIcon;

        private Color defaultBorderColor = new Color(0.098f, 0.098f, 0.098f);
        private Color white = new Color32(255, 255, 255, 255);

        public BTGNodeData(FullNodeInfo mainNodeDetails, bool entryPoint, GraphView.Port parentPort, List<FullNodeInfo> decoratorData)
        {
            MainNodeDetails = mainNodeDetails;
            DecoratorData = decoratorData;
            MainNodeDetails.RuntimeNode.NodeStatusChanged += OnNodeStatusChanged;

            title = string.IsNullOrEmpty(MainNodeDetails.RuntimeNode.Name) ? MainNodeDetails.RuntimeNode.GetType().Name : MainNodeDetails.RuntimeNode.Name;
            Id = Guid.NewGuid().ToString();
            EntryPoint = entryPoint;
            ParentPort = parentPort;

            statusIcon = new Image()
            {
                style =
                {
                    width = 25,
                    height = 25,
                    marginRight = 5,
                    marginTop = 5
                }
            };

            statusIcon.tintColor = white;
            titleContainer.Add(statusIcon);

            nodeBorder = this.Q<VisualElement>("node-border");
            nodeTitleContainer = this.Q<VisualElement>("title");
            nodeTitleContainer.style.backgroundColor = new StyleColor(MainNodeDetails.PropertyData.TitleBarColor.WithAlpha(BehaviorTreeEditorWindow.SettingsData.GetDimLevel()));

            nodeTopMessageGeneral = GenerateStatusMessageLabel("generalStatusMessage", DisplayStyle.None);
            nodeTopMessageDecorator = GenerateStatusMessageLabel("decoratorReason", DisplayStyle.None);
            nodeLastEvaluatedTimeStamp = GenerateStatusMessageLabel("lastEvalTimeStamp", DisplayStyle.None);

            if (DecoratorData != null)
            {
                foreach(var decorator in DecoratorData)
                {
                    decorator.RuntimeNode.NodeStatusChanged += OnNodeStatusChanged;

                    Image decoratorImage = CreateDecoratorImage(decorator.PropertyData.Icon.texture);

                    nodeTitleContainer.Add(decoratorImage);
                    decoratorImage.SendToBack();
                }
            }

            this.Q<VisualElement>("contents").Add(nodeTopMessageGeneral);
            this.Q<VisualElement>("contents").Add(nodeTopMessageDecorator);
            this.Q<VisualElement>("contents").Add(nodeLastEvaluatedTimeStamp);
            nodeLastEvaluatedTimeStamp.SendToBack();
            nodeTopMessageGeneral.SendToBack();
            nodeTopMessageDecorator.SendToBack();

            OnNodeStatusChanged(MainNodeDetails.RuntimeNode);

            if (DecoratorData != null)
                DecoratorData.ForEach(x => OnNodeStatusChanged(x.RuntimeNode));
        }

        private Image CreateDecoratorImage(Texture texture)
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
            icon.image = texture;

            return icon;
        }

        private Label GenerateStatusMessageLabel(string name, DisplayStyle display)
        {
            return new Label()
            {
                name = name,
                style =
                {
                    color = white,
                    backgroundColor = new Color(.17f,.17f,.17f),
                    flexWrap = Wrap.Wrap,
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    display = display,
                    whiteSpace = WhiteSpace.Normal
                }
            };
        }

        public void AddPort(GraphView.Port port, string name, bool isInputPort)
        {
            port.portColor = Color.white;
            port.portName = name;

            if (isInputPort)
            {
                inputContainer.Add(port);
                InputPort = port;
            }
            else
            {
                outputContainer.Add(port);
                OutputPorts.Add(port);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        public void GenerateEdge()
        {
            var tempEdge = new GraphView.Edge()
            {
                output = ParentPort,
                input = InputPort
            };

            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);

            Add(tempEdge);

            RefreshExpandedState();
            RefreshPorts();
        }

        private void OnNodeStatusChanged(NodeBase sender)
        {
            if (BehaviorTreeEditorWindow.SettingsData.DataFile.LastRunTimeStamp)
            {
                nodeLastEvaluatedTimeStamp.text = $"Last evaluated at {DateTime.Now:h:mm:ss:fff tt}";
                nodeLastEvaluatedTimeStamp.style.display = DisplayStyle.Flex;
            }

            if (MainNodeDetails.RuntimeNode != sender)
            {
                if (sender.StatusReason == "")
                {
                    nodeTopMessageDecorator.style.display = DisplayStyle.None;
                }
                else
                {
                    nodeTopMessageDecorator.style.display = DisplayStyle.Flex;
                    nodeTopMessageDecorator.text = sender.StatusReason;
                }
            }
            else
            {
                if (sender.StatusReason == "")
                {
                    nodeTopMessageGeneral.style.display = DisplayStyle.None;
                }
                else
                {
                    nodeTopMessageGeneral.style.display = DisplayStyle.Flex;
                    nodeTopMessageGeneral.text = sender.StatusReason;
                }
            }

            statusIcon.style.visibility = UnityEngine.UIElements.Visibility.Visible;
            ColorPorts(white);
            DefaultBorder();

            switch (sender.LastNodeStatus)
            {
                case NodeStatus.Failure:
                    if (BehaviorTreeEditorWindow.SettingsData.DataFile.FailureIcon != null && BehaviorTreeEditorWindow.SettingsData.DataFile.SuccessIcon != null)
                        UpdateStatusIcon(MainNodeDetails.PropertyData.InvertResult ? BehaviorTreeEditorWindow.SettingsData.DataFile.SuccessIcon.texture :
                                         BehaviorTreeEditorWindow.SettingsData.DataFile.FailureIcon.texture);
                    nodeTitleContainer.style.backgroundColor = new StyleColor(MainNodeDetails.PropertyData.TitleBarColor.WithAlpha(BehaviorTreeEditorWindow.SettingsData.GetDimLevel()));
                    break;
                case NodeStatus.Success:
                    if (BehaviorTreeEditorWindow.SettingsData.DataFile.FailureIcon != null && BehaviorTreeEditorWindow.SettingsData.DataFile.SuccessIcon != null)
                        UpdateStatusIcon(MainNodeDetails.PropertyData.InvertResult ? BehaviorTreeEditorWindow.SettingsData.DataFile.FailureIcon.texture :
                                         BehaviorTreeEditorWindow.SettingsData.DataFile.SuccessIcon.texture);
                    nodeTitleContainer.style.backgroundColor = new StyleColor(MainNodeDetails.PropertyData.TitleBarColor.WithAlpha(BehaviorTreeEditorWindow.SettingsData.GetDimLevel()));
                    break;
                case NodeStatus.Running:
                    if (BehaviorTreeEditorWindow.SettingsData.DataFile.RunningIcon != null)
                        UpdateStatusIcon(BehaviorTreeEditorWindow.SettingsData.DataFile.RunningIcon.texture);
                    nodeTitleContainer.style.backgroundColor = new StyleColor(MainNodeDetails.PropertyData.TitleBarColor.WithAlpha(1f));
                    ColorPorts(BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor);
                    RunningBorder();
                    break;
                case NodeStatus.Unknown:
                    nodeTitleContainer.style.backgroundColor = new StyleColor(MainNodeDetails.PropertyData.TitleBarColor.WithAlpha(1f));
                    statusIcon.style.visibility = UnityEngine.UIElements.Visibility.Hidden;
                    break;
            }
        }

        private void UpdateStatusIcon(Texture newImage)
        {
            if (newImage != null)
                statusIcon.image = newImage;
        }

        private void ColorPorts(Color color)
        {
            if (InputPort != null)
                InputPort.portColor = color;

            if (ParentPort != null)
                ParentPort.portColor = color;
        }

        private void RunningBorder()
        {
            nodeBorder.style.borderLeftColor = BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor;
            nodeBorder.style.borderRightColor = BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor;
            nodeBorder.style.borderTopColor = BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor;
            nodeBorder.style.borderBottomColor = BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor;
            nodeBorder.style.borderTopWidth = 2f;
            nodeBorder.style.borderRightWidth = 2f;
            nodeBorder.style.borderLeftWidth = 2f;
            nodeBorder.style.borderBottomWidth = 2f;
        }

        private void DefaultBorder()
        {
            nodeBorder.style.borderLeftColor = defaultBorderColor;
            nodeBorder.style.borderRightColor = defaultBorderColor;
            nodeBorder.style.borderTopColor = defaultBorderColor;
            nodeBorder.style.borderBottomColor = defaultBorderColor;
            nodeBorder.style.borderTopWidth = 1f;
            nodeBorder.style.borderRightWidth = 1f;
            nodeBorder.style.borderLeftWidth = 1f;
            nodeBorder.style.borderBottomWidth = 1f;
        }
    }
}