using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using static Archi.BT.Visualizer.SettingsData;

namespace Archi.BT.Visualizer
{
    public enum SettingNodeType
    {
        Base,
        Main,
        Override
    }

    public class SettingNodeRow : VisualElement
    {
        public static VisualTreeAsset rowTemplate;
        public static Color DeactivatedColor = new Color32(56, 56, 56, 255);

        private ObjectField nodeScript;
        private ColorField nodeColor;
        private ObjectField nodeIcon;
        private VisualElement imagePreview;
        private Button deleteButton;
        private Toggle isDecorator;
        private Toggle invertResult;

        SettingNodeType nodeType;
        private NodeProperty settings;
        public NodeProperty Settings
        {
            get => settings;
            set
            {
                switch (nodeType)
                {
                    case SettingNodeType.Base:
                        BehaviorTreeEditorWindow.SettingsData.SetDefaultStyle(value);
                        break;
                    case SettingNodeType.Main:
                        BehaviorTreeEditorWindow.SettingsData.SetMainOrOverrideStyle("MainStyleProperties", value, settings == null ? null : settings.Script);
                        break;
                    case SettingNodeType.Override:
                        BehaviorTreeEditorWindow.SettingsData.SetMainOrOverrideStyle("OverrideStyleProperties", value, settings == null ? null : settings.Script);
                        break;
                }

                settings = value;
            }
        }

        public SettingNodeRow(NodeProperty settings, SettingNodeType nodeType)
        {
            if (rowTemplate == null)
                rowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BehaviorTreeEditorWindow.c_WindowPath + "setting_node_row.uxml");

            Add(rowTemplate.CloneTree());

            this.nodeType = nodeType;
            this.settings = settings;

            nodeScript = this.Q<ObjectField>("script");
            nodeColor = this.Q<ColorField>("color");
            nodeIcon = this.Q<ObjectField>("icon");
            imagePreview = this.Q<VisualElement>("imgPreview");
            deleteButton = this.Q<Button>("btnDelete");
            isDecorator = this.Q<Toggle>("isDecorator");
            invertResult = this.Q<Toggle>("invertResults");

            nodeIcon.objectType = typeof(Sprite);
            nodeScript.objectType = typeof(MonoScript);

            if(nodeType == SettingNodeType.Base)
            {
                nodeScript.parent.Remove(nodeScript);
                deleteButton.parent.Remove(deleteButton);
                isDecorator.parent.Remove(isDecorator);
                invertResult.parent.Remove(invertResult);

                nodeColor.style.marginLeft = 2;
            }
            else
            {
                isDecorator.value = settings.IsDecorator;
                invertResult.value = settings.InvertResult;
                nodeScript.value = settings.Script;

                deleteButton.clicked += Delete_OnClick;

                isDecorator.RegisterValueChangedCallback((e) =>
                {
                    NodeProperty original = settings;
                    original.IsDecorator = e.newValue;
                    settings = original;

                    ToggleColorField(!e.newValue);
                });

                invertResult.RegisterValueChangedCallback((e) =>
                {
                    NodeProperty original = settings;
                    original.InvertResult = e.newValue;
                    settings = original;
                });

                nodeScript.RegisterValueChangedCallback((e) =>
                {
                    NodeProperty original = settings;
                    original.Script = e.newValue as MonoScript;
                    settings = original;
                });
            }

            ToggleColorField(!settings.IsDecorator);

            if (settings.IsDecorator)
                nodeColor.value = settings.TitleBarColor;

            if (settings.Icon != null)
            {
                nodeIcon.value = settings.Icon;
                imagePreview.style.backgroundImage = new StyleBackground(settings.Icon.texture);
            }

            nodeColor.RegisterValueChangedCallback((e) =>
            {
                NodeProperty original = settings;
                original.TitleBarColor = e.newValue;
                settings = original;
            });

            nodeIcon.RegisterValueChangedCallback((e) =>
            {
                Sprite newIcon = e.newValue as Sprite;
                NodeProperty original = settings;
                original.Icon = newIcon;
                settings = original;

                imagePreview.style.backgroundImage = new StyleBackground(newIcon.texture);
            });
        }

        public void Delete_OnClick()
        {
            string styleType = nodeType == SettingNodeType.Main ? "MainStyleProperties" : "OverrideStyleProperties";
            BehaviorTreeEditorWindow.SettingsData.RemoveMainOrOverrideStyle(styleType, settings.Script);
            this.parent.Remove(this);
        }

        private void ToggleColorField(bool enabled)
        {
            nodeColor.SetEnabled(enabled);
            nodeColor.value = enabled ? DefaultColor : DeactivatedColor;
        }
    }

}
