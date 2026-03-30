#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class GameSceneGeneratorScript : EditorWindow
{
    [MenuItem("Tools/Generate Scene")]
    public static void ShowWindow()
    {
        GetWindow<GameSceneGeneratorScript>("Scene Generator");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Generate Scene"))
        {
            GenerateScene();
        }
    }

    void GenerateScene()
    {
        Debug.Log("Scene Generated!");
    }
}
#endif