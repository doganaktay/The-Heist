using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    // parameters
    SerializedObject so;
    SerializedProperty _gridSizeX, _gridSizeY;
    SerializedProperty _cellSizeX, _cellSizeY;
    SerializedProperty _parameterDeviation, _biasMultipliersMin, _biasMultipliersMax;
    SerializedProperty _seed, _reuseSeed, _seedFromSave;

    // references
    SerializedProperty _touchControl, _trajectory, _physicsSim, _lights;
    SerializedProperty _pathfinder, _areafinder, _spotfinder, _graphFinder;
    SerializedProperty _propagationModule, _guardManager, _curator, _cctv;
    SerializedProperty _textOverlay, _playerPrefab, _mazePrefab, _mazeInstance;
    SerializedProperty _layout, _spawnedObjects, _cctvHolder;

    bool showReferences = false;

    public static RNG rng;

    private void OnEnable()
    {
        if (rng == null)
            rng = new RNG((uint)System.DateTime.Now.Ticks);

        so = serializedObject;

        // params
        _gridSizeX = so.FindProperty("gridSizeX");
        _gridSizeY = so.FindProperty("gridSizeY");
        _cellSizeX = so.FindProperty("cellSizeX");
        _cellSizeY = so.FindProperty("cellSizeY");
        _parameterDeviation = so.FindProperty("parameterDeviation");
        _biasMultipliersMin = so.FindProperty("biasMultipliers").FindPropertyRelative("min");
        _biasMultipliersMax = so.FindProperty("biasMultipliers").FindPropertyRelative("max");
        _seed = so.FindProperty("seed");
        _reuseSeed = so.FindProperty("reuseSeed");
        _seedFromSave = so.FindProperty("seedFromSave");

        // references
        _touchControl = so.FindProperty("touchControl");
        _trajectory = so.FindProperty("trajectory");
        _physicsSim = so.FindProperty("physicsSim");
        _lights = so.FindProperty("lights");
        _pathfinder = so.FindProperty("pathfinder");
        _areafinder = so.FindProperty("areafinder");
        _spotfinder = so.FindProperty("spotfinder");
        _graphFinder = so.FindProperty("graphFinder");
        _propagationModule = so.FindProperty("propagationModule");
        _guardManager = so.FindProperty("guardManager");
        _curator = so.FindProperty("curator");
        _cctv = so.FindProperty("cctv");
        _textOverlay = so.FindProperty("textOverlay");
        _playerPrefab = so.FindProperty("playerPrefab");
        _mazePrefab = so.FindProperty("mazePrefab");
        _mazeInstance = so.FindProperty("mazeInstance");
        _layout = so.FindProperty("layout");
        _spawnedObjects = so.FindProperty("spawnedObjects");
        _cctvHolder = so.FindProperty("cctvHolder");
    }

    public override void OnInspectorGUI()
    {
        //GUIStyles
        var centered = new GUIStyle(GUI.skin.GetStyle("Label"));
        centered.alignment = TextAnchor.MiddleCenter;
        centered.fontStyle = FontStyle.Bold;
        centered.fontSize = 20;

        var title = new GUIStyle();
        title.alignment = TextAnchor.MiddleLeft;
        title.fontStyle = FontStyle.Bold;
        title.fontSize = 15;

        var subtitle = new GUIStyle();
        subtitle.alignment = TextAnchor.LowerLeft;
        subtitle.fontStyle = FontStyle.Bold;

        var foldout = new GUIStyle(GUI.skin.GetStyle("Foldout"));
        foldout.padding.left = 15;
        foldout.alignment = TextAnchor.LowerLeft;
        foldout.fontStyle = FontStyle.Bold;

        using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Game Manager", centered);
        }

        GUILayout.Space(10);

        so.Update();

        GUILayout.Label("Maze", title);
        GUILayout.Space(2);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Grid Size -> X:", GUILayout.Width(85));
            EditorGUILayout.PropertyField(_gridSizeX, GUIContent.none, GUILayout.Width(30));

            GUILayout.Label("Y:", GUILayout.Width(15));
            EditorGUILayout.PropertyField(_gridSizeY, GUIContent.none, GUILayout.Width(30));

            GUILayout.Label("Controls size of 2D cell array", EditorStyles.helpBox);
        }
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Cell Size -> X:", GUILayout.Width(85));
            EditorGUILayout.PropertyField(_cellSizeX, GUIContent.none, GUILayout.Width(30));

            GUILayout.Label("Y:", GUILayout.Width(15));
            EditorGUILayout.PropertyField(_cellSizeY, GUIContent.none, GUILayout.Width(30));

            GUILayout.Label("Controls cell scale for resolution", EditorStyles.helpBox);
        }

        GUILayout.Space(2);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(2);

        GUILayout.Label("Parameter Deviation", title);
        GUILayout.Space(2);

      
        using (new GUILayout.VerticalScope())
        {
            GUILayout.Label("-Bias   <-    Sigma    ->   +Bias", GUILayout.Width(170));

            using (new GUILayout.HorizontalScope(GUILayout.Width(170)))
            {
                EditorGUILayout.PropertyField(_biasMultipliersMin, GUIContent.none, GUILayout.Width(40));
                GUILayout.Label("", GUILayout.Width(20));
                EditorGUILayout.PropertyField(_parameterDeviation, GUIContent.none, GUILayout.Width(40));
                GUILayout.Label("", GUILayout.Width(20));
                EditorGUILayout.PropertyField(_biasMultipliersMax, GUIContent.none, GUILayout.Width(40));
            }
        }

        GUILayout.Label("Used to scale any game parameter. Takes single input and returns range [Input-+ (Sigma * -+Bias)]", EditorStyles.helpBox);

        GUILayout.Space(2);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(2);

        GUILayout.Label("RNG", title);
        GUILayout.Space(2);

        GUILayout.Label("Procedural generation uses seeded RNG", EditorStyles.helpBox);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Seed: ", GUILayout.Width(35));
            EditorGUILayout.PropertyField(_seed, GUIContent.none);
        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate"))
            {
                GameManager gm = target as GameManager;
                gm.GenerateSeed();
            }

            if (GUILayout.Button("Save"))
            {
                GameManager gm = target as GameManager;
                gm.SaveSeed(gm.seed);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Seed -> ", GUILayout.Width(50));
            GUILayout.Label("Reuse:", GUILayout.Width(40));
            EditorGUILayout.PropertyField(_reuseSeed, GUIContent.none, GUILayout.Width(20));
            GUILayout.Label("From Save:", GUILayout.Width(65));
            EditorGUILayout.PropertyField(_seedFromSave, GUIContent.none, GUILayout.Width(20));
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        showReferences = EditorGUILayout.Foldout(showReferences, "References", true, foldout);
        if (showReferences)
        {
            EditorGUILayout.PropertyField(_touchControl);
            EditorGUILayout.PropertyField(_trajectory);
            EditorGUILayout.PropertyField(_physicsSim);
            EditorGUILayout.PropertyField(_lights);
            EditorGUILayout.PropertyField(_pathfinder);
            EditorGUILayout.PropertyField(_areafinder);
            EditorGUILayout.PropertyField(_spotfinder);
            EditorGUILayout.PropertyField(_graphFinder);
            EditorGUILayout.PropertyField(_propagationModule);
            EditorGUILayout.PropertyField(_guardManager);
            EditorGUILayout.PropertyField(_curator);
            EditorGUILayout.PropertyField(_cctv);
            EditorGUILayout.PropertyField(_textOverlay);
            EditorGUILayout.PropertyField(_playerPrefab);
            EditorGUILayout.PropertyField(_mazePrefab);
            EditorGUILayout.PropertyField(_mazeInstance);
            EditorGUILayout.PropertyField(_layout);
            EditorGUILayout.PropertyField(_spawnedObjects);
            EditorGUILayout.PropertyField(_cctvHolder);
        }

        so.ApplyModifiedProperties();

    }
}