using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Momiq qo'zichoq o'yini uchun sahna yaratadi (Tools > Momiq > Create Scene).</summary>
public static class MomiqInstaller
{
    [MenuItem("Tools/Momiq/Create Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject("Momiq");
        go.AddComponent<MomiqController>();

        string dir = "Assets/_Scenes";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "_Scenes");
        string path = dir + "/Momiq.unity";
        EditorSceneManager.SaveScene(scene, path);

        EditorUtility.DisplayDialog("Tayyor",
            "Momiq sahnasi yaratildi:\n" + path + "\n\nPlay bosib gapiradigan qo'zichoqni sinang.", "OK");
    }
}
