using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Archi.BT.Visualizer
{

    public class BehaviorTreeEditorWindow : EditorWindow
    {
        public static BehaviorTreeEditorWindow Instance;

        public static readonly string c_RootPath = "Assets/BehaviorTree/Editor";
        public static readonly string c_RootDataPath = "Assets/BehaviorTree/Data";
        public static readonly string c_WindowPath = $"{c_RootPath}/Windows/";
        public static readonly string c_DataPath = $"{c_RootDataPath}/Resources";
        public static readonly string c_StylePath = $"{c_RootPath}/Styles/BTGraphStyleSheet.uss";
        public static readonly string c_SpritePath = $"{c_RootPath}/Sprites/";

        public static MiniMap MiniMap;
        public List<Type> ScriptsWithBehaviorTrees = new List<Type>();

        public static BehaviorTreeGraphView GraphView;
        public static DataManager SettingsData;
        private ToolbarMenu activeTreesInScene;
        private List<UnityEngine.Object> sceneNodes = new List<UnityEngine.Object>();
        private SettingsWindow settingsWindow;

        [MenuItem("Tools/Behavior Tree Visualizer")]
        public static void Init()
        {
            SettingsData = new DataManager();
            Instance = GetWindow<BehaviorTreeEditorWindow>();
            Instance.titleContent = new GUIContent("Behavior Tree Visualizer");
            Instance.minSize = new Vector2(500, 500);

            Instance.rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(c_StylePath));
        }

        public static void DrawBehaviorTree(NodeBase tree, bool focusWindow)
        {
            if (!HasOpenInstances<BehaviorTreeEditorWindow>())
                return;

            if (focusWindow)
                FocusWindowIfItsOpen(typeof(BehaviorTreeEditorWindow));

            GraphView.ClearTree();
            GraphView.LoadBehaviorTree(tree);
        }

        private void OnEnable()
        {
            if (SettingsData == null)
                SettingsData = new DataManager();

            ConstructGraphView();
            GenerateToolbar();
            GenerateMinimap();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(GraphView);
        }

        private async void GenerateMinimap()
        {
            await Task.Delay(100);

            MiniMap = new MiniMap { anchored = false };
            MiniMap.SetPosition(new Rect(10, 30, 200, 140));
            GraphView.Add(MiniMap);

            ToggleMinimap(SettingsData.DataFile.EnableMiniMap);

        }

        public void ToggleMinimap(bool visible)
        {
            MiniMap.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ConstructGraphView()
        {
            GraphView = new BehaviorTreeGraphView()
            {
                name = "Behavior Graph"
            };

            GraphView.StretchToParentSize();
            rootVisualElement.Add(GraphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();
            activeTreesInScene = new ToolbarMenu();
            activeTreesInScene.text = "Select Behavior Tree";

            Button scanScene = new Button(() => ScanScene());
            scanScene.text = "Scan Scene";
            scanScene.tooltip = "Scan scene for available behavior trees";

            Button settingsButton = new Button(() =>
            {
                if (settingsWindow != null)
                    settingsWindow.Close();

                settingsWindow = GetWindow<SettingsWindow>();
                settingsWindow.titleContent = new GUIContent("Settings");
                settingsWindow.minSize = new Vector2(500, 500);
            });
            settingsButton.text = "Settings";

            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(activeTreesInScene);
            toolbar.Add(scanScene);
            toolbar.Add(settingsButton);

            rootVisualElement.Add(toolbar);
        }

        private void ScanScene()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("Scan only works when game is running.");
                return;
            }

            for(int i = activeTreesInScene.menu.MenuItems().Count - 1; i == 0; i--)
            {
                activeTreesInScene.menu.RemoveItemAt(i);
            }

            ScanProjectForTreeReferences();

            if (ScriptsWithBehaviorTrees == null || ScriptsWithBehaviorTrees.Count == 0)
                return;

            foreach(Type script in ScriptsWithBehaviorTrees)
            {
                UnityEngine.Object[] objectsWithScript = GameObject.FindObjectsOfType(script);
                sceneNodes = new List<UnityEngine.Object>();

                foreach(var item in objectsWithScript)
                {
                    if(sceneNodes.FirstOrDefault(x => x == item) == null)
                    {
                        activeTreesInScene.menu.InsertAction(activeTreesInScene.menu.MenuItems().Count, item.name,
                        (e) =>
                        {
                            UnityEngine.Object runtimeData = e.userData as UnityEngine.Object;
                            activeTreesInScene.text = runtimeData.name;
                            GraphView.ClearTree();

                            PropertyInfo nodeToRun = runtimeData.GetType().GetProperty("BehaviorTree");
                            GraphView.LoadBehaviorTree(nodeToRun.GetValue(runtimeData) as NodeBase);
                        },
                        (e) =>
                        {
                            if (activeTreesInScene.text.Equals(e.name))
                                return DropdownMenuAction.Status.Checked;

                            return DropdownMenuAction.Status.Normal;
                        }, item);

                        sceneNodes.Add(item);
                    }
                }
            }
        }

        private void ScanProjectForTreeReferences()
        {
            ScriptsWithBehaviorTrees.Clear();

            Assembly defaultAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp");
            ScriptsWithBehaviorTrees = defaultAssembly?.GetTypes()
                                       .Where(x => x.IsClass && x.GetInterfaces().Contains(typeof(IBehaviorTree))
                                      && x.IsSubclassOf(typeof(UnityEngine.Object))).ToList();

            if (ScriptsWithBehaviorTrees == null || ScriptsWithBehaviorTrees.Count == 0)
                Debug.LogError("No behavior trees found in assembly");
        }
    }
}
