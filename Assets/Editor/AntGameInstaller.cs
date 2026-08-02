using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Chumoli o'yini uchun sahna yaratadi (Tools > Ant Game > Create Scene).</summary>
public static class AntGameInstaller
{
    [MenuItem("Tools/Ant Game/Create Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject("AntGame");
        go.AddComponent<AntGameController>();

        string dir = "Assets/_Scenes";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "_Scenes");
        string path = dir + "/AntGame.unity";
        EditorSceneManager.SaveScene(scene, path);

        EditorUtility.DisplayDialog("Tayyor",
            "AntGame sahnasi yaratildi:\n" + path + "\n\nPlay bosib chumoli o'yinini sinang.", "OK");
    }
}
