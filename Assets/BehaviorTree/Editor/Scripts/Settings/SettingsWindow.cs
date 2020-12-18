using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Archi.BT.Visualizer
{
    public class SettingsWindow : EditorWindow
    {
        private static VisualTreeAsset rootTemplate;
        private static Slider dimLevel;
        private static ColorField colorField;
        private static Toggle miniMap;
        private static Toggle lastTimestamp;
        private static ObjectField successIcon;
        private static ObjectField runningIcon;
        private static ObjectField failureIcon;
        private static SettingNodeRow baseNode;
        private static ObjectField mainNode;
        private static ObjectField overrideNode;
        private static VisualElement mainNodesContainer;
        private static VisualElement overrideNodesContainer;

        private void OnEnable()
        {
            rootTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BehaviorTreeEditorWindow.c_WindowPath + "Settings.uxml");
            rootVisualElement.Add(rootTemplate.CloneTree());

            dimLevel = rootVisualElement.Q<Slider>("Slider_Inactive_Dim");
            colorField = rootVisualElement.Q<ColorField>("clr_BorderHighlight");
            miniMap = rootVisualElement.Q<Toggle>("bool_MiniMap");
            lastTimestamp = rootVisualElement.Q<Toggle>("bool_LastEvalTimestamp");
            successIcon = rootVisualElement.Q<ObjectField>("successIcon");
            runningIcon = rootVisualElement.Q<ObjectField>("runningIcon");
            failureIcon = rootVisualElement.Q<ObjectField>("failureIcon");
            mainNode = rootVisualElement.Q<ObjectField>("mainNodeSelector");
            overrideNode = rootVisualElement.Q<ObjectField>("overrideNodeSelector");
            baseNode = new SettingNodeRow(BehaviorTreeEditorWindow.SettingsData.DataFile.DefaultStyleProperties, SettingNodeType.Base);
            mainNodesContainer = rootVisualElement.Q<VisualElement>("mainNodesContainer");
            overrideNodesContainer = rootVisualElement.Q<VisualElement>("overridesContainer");

            rootVisualElement.Q<VisualElement>("baseNodesContainer").Add(baseNode);

            mainNode.objectType = typeof(MonoScript);
            overrideNode.objectType = typeof(MonoScript);
            successIcon.objectType = typeof(Sprite);
            failureIcon.objectType = typeof(Sprite);
            runningIcon.objectType = typeof(Sprite);

            miniMap.RegisterValueChangedCallback((e) => { ToggleMiniMap(e.newValue); });
            lastTimestamp.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.SetLastEvalTimeStamp(e.newValue); });
            dimLevel.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.SetDimLevel(e.newValue); });
            colorField.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.SetBorderHighlightColor(e.newValue); });
            successIcon.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.UpdateIcon(e.newValue as Sprite, IconType.Success); });
            failureIcon.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.UpdateIcon(e.newValue as Sprite, IconType.Failure); });
            runningIcon.RegisterValueChangedCallback((e) => { BehaviorTreeEditorWindow.SettingsData.UpdateIcon(e.newValue as Sprite, IconType.Running); });
            overrideNode.RegisterValueChangedCallback((e) =>
            {
                if (e.newValue != null)
                {
                    SettingNodeRow newRow = new SettingNodeRow(new NodeProperty() { Script = e.newValue as MonoScript }, SettingNodeType.Override);
                    overrideNodesContainer.Add(newRow);
                    newRow.PlaceBehind(overrideNode);

                    overrideNode.value = null;
                }
            });
            mainNode.RegisterValueChangedCallback((e) =>
            {
                if (e.newValue != null)
                {
                    SettingNodeRow newRow = new SettingNodeRow(new NodeProperty() { Script = e.newValue as MonoScript }, SettingNodeType.Main);
                    mainNodesContainer.Add(newRow);
                    newRow.PlaceBehind(mainNode);

                    mainNode.value = null;
                }
            });
        }

        public static void ToggleMiniMap(bool enabled)
        {
            BehaviorTreeEditorWindow.SettingsData.SetMinimap(enabled);
            BehaviorTreeEditorWindow.Instance.ToggleMinimap(enabled);
        }

        public static void DisplaySettings()
        {
            dimLevel.value = BehaviorTreeEditorWindow.SettingsData.DataFile.DimLevel;
            colorField.value = BehaviorTreeEditorWindow.SettingsData.DataFile.BorderHighlightColor;
            miniMap.value = BehaviorTreeEditorWindow.SettingsData.DataFile.EnableMiniMap;
            lastTimestamp.value = BehaviorTreeEditorWindow.SettingsData.DataFile.LastRunTimeStamp;
            successIcon.value = BehaviorTreeEditorWindow.SettingsData.DataFile.SuccessIcon;
            failureIcon.value = BehaviorTreeEditorWindow.SettingsData.DataFile.FailureIcon;
            runningIcon.value = BehaviorTreeEditorWindow.SettingsData.DataFile.RunningIcon;

            for(int i = 0; i < BehaviorTreeEditorWindow.SettingsData.DataFile.OverrideStyleProperties.Count; i++)
            {
                SettingNodeRow newRow = new SettingNodeRow(BehaviorTreeEditorWindow.SettingsData.DataFile.OverrideStyleProperties[i], SettingNodeType.Override);
                overrideNodesContainer.Add(newRow);
                newRow.PlaceBehind(overrideNode);
            }

            for (int i = 0; i < BehaviorTreeEditorWindow.SettingsData.DataFile.MainStyleProperties.Count; i++)
            {
                SettingNodeRow newRow = new SettingNodeRow(BehaviorTreeEditorWindow.SettingsData.DataFile.MainStyleProperties[i], SettingNodeType.Main);
                mainNodesContainer.Add(newRow);
                newRow.PlaceBehind(mainNode);
            }
        }
    }

}
