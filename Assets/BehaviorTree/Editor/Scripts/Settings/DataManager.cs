using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using Archi.BT.Visualizer;
using static Archi.BT.Visualizer.SettingsData;

public class DataManager
{
    public SettingsData DataFile { get; set; }
    private SerializedObject settingsRef;

    public DataManager()
    {
        LoadSettingsFile();
        settingsRef = new SerializedObject(DataFile);
    }

    public void SetDefaultStyle(NodeProperty newProperties)
    {
        if(settingsRef == null)
        {
            Debug.Log("Settings cannot be loaded");
            return;
        }

        settingsRef.FindProperty("DefaultStyleProperties").FindPropertyRelative("TitleBarColor").colorValue = newProperties.TitleBarColor;
        settingsRef.FindProperty("DefaultStyleProperties").FindPropertyRelative("Icon").objectReferenceValue = newProperties.Icon;

        SaveSettingsFile();
    }

    public void SetMainOrOverrideStyle(string propName, NodeProperty newValue, MonoScript originalScript = null)
    {
        if (settingsRef == null)
        {
            Debug.Log("Settings cannot be loaded");
            return;
        }

        var property = settingsRef.FindProperty(propName);
        int index = property.arraySize;

        for(int i = 0; i < property.arraySize; i++)
        {
            MonoScript script = property.GetArrayElementAtIndex(i).FindPropertyRelative("Script").objectReferenceValue as MonoScript;

            if(script == originalScript || script == newValue.Script)
            {
                index = i;
                break;
            }
        }

        if (index == property.arraySize)
            property.InsertArrayElementAtIndex(index);

        property.GetArrayElementAtIndex(index).FindPropertyRelative("Script").objectReferenceValue = newValue.Script;
        property.GetArrayElementAtIndex(index).FindPropertyRelative("TitleBarColor").colorValue = newValue.TitleBarColor;
        property.GetArrayElementAtIndex(index).FindPropertyRelative("Icon").objectReferenceValue = newValue.Icon;
        property.GetArrayElementAtIndex(index).FindPropertyRelative("IsDecorator").boolValue = newValue.IsDecorator;
        property.GetArrayElementAtIndex(index).FindPropertyRelative("InvertResult").boolValue = newValue.InvertResult;

        SaveSettingsFile();
    }

    internal void SetBorderHighlightColor(Color newValue)
    {
        settingsRef.FindProperty("BorderHighlightColor").colorValue = newValue;
        SaveSettingsFile();
    }

    public void SetLastEvalTimeStamp(bool newValue)
    {
        settingsRef.FindProperty("LastRunTimeStamp").boolValue = newValue;
        SaveSettingsFile();
    }

    public void SetDimLevel(float newValue)
    {
        settingsRef.FindProperty("DimLevel").floatValue = newValue;
        SaveSettingsFile();
    }

    public void SetMinimap(bool newValue)
    {
        settingsRef.FindProperty("EnableMiniMap").boolValue = newValue;
        SaveSettingsFile();
    }

    public void UpdateIcon(Sprite newIcon, IconType iconType)
    {
        if (settingsRef == null)
        {
            Debug.Log("Settings cannot be loaded");
            return;
        }

        switch (iconType)
        {
            case IconType.Failure:
                settingsRef.FindProperty("FailureIcon").objectReferenceValue = newIcon;
                break;
            case IconType.Running:
                settingsRef.FindProperty("RunningIcon").objectReferenceValue = newIcon;
                break;
            case IconType.Success:
                settingsRef.FindProperty("SuccessIcon").objectReferenceValue = newIcon;
                break;
        }

        SaveSettingsFile();
    }

    public void RemoveMainOrOverrideStyle(string propName, MonoScript existingScript)
    {
        if (settingsRef == null)
        {
            Debug.Log("Settings cannot be loaded");
            return;
        }

        var property = settingsRef.FindProperty(propName);
        int index = -1;

        for(int i = 0; i < property.arraySize; i++)
        {
            var script = property.GetArrayElementAtIndex(i).FindPropertyRelative("Script").objectReferenceValue as MonoScript;

            if(script == existingScript)
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
            property.DeleteArrayElementAtIndex(index);

        SaveSettingsFile();
    }

    public NodeProperty GetNodeStyleDetails(object scriptToFind)
    {
        NodeProperty node = DataFile.OverrideStyleProperties.FirstOrDefault(x => scriptToFind.GetType().Equals(x.Script.GetClass()));

        if (node == null)
            node = DataFile.MainStyleProperties.FirstOrDefault(x => scriptToFind.GetType().IsSubclassOf(x.Script.GetClass())
            || scriptToFind.GetType().Equals(x.Script.GetClass()));

        if (node == null)
            node = DataFile.DefaultStyleProperties;

        return node;
    }

    public float GetDimLevel()
    {
        return DataFile.DimLevel / 255;
    }

    private void SaveSettingsFile()
    {
        EditorUtility.SetDirty(DataFile);
        settingsRef.ApplyModifiedProperties();
    }

    public void LoadSettingsFile()
    {
        SettingsData data = AssetDatabase.LoadAssetAtPath<SettingsData>($"{BehaviorTreeEditorWindow.c_DataPath}/settings.asset");

        if (data == null)
        {
            DataFile = ScriptableObject.CreateInstance("SettingsData") as SettingsData;
            CreateSettingsFile();
        }
        else
        {
            DataFile = data;
        }
    }

    private void CreateSettingsFile()
    {
        if (!AssetDatabase.IsValidFolder(BehaviorTreeEditorWindow.c_RootDataPath))
        {
            AssetDatabase.CreateFolder("Assets", "Behaviour Tree Visualizer");
            AssetDatabase.CreateFolder(BehaviorTreeEditorWindow.c_RootDataPath, "Resources");
        }

        if (!AssetDatabase.Contains(DataFile))
        {
            AssetDatabase.CreateAsset(DataFile, $"{BehaviorTreeEditorWindow.c_DataPath}/settings.asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
