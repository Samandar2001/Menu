using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// O'yin tizimini sahnaga o'rnatadi: sahnada BookGameController bo'lishini ta'minlaydi
/// va sahifalardagi eski "GameButton" larni tozalaydi (ular varaqlashga xalaqit berardi).
/// Doimiy "O'YIN" tugmasini kontrollerning o'zi runtime'da yaratadi (ekran burchagida).
/// BookPro / BookController logikasiga tegmaydi.
/// </summary>
public static class BookGamesInstaller
{
    [MenuItem("Tools/Book/Optimize Textures (Web)")]
    public static void OptimizeTextures()
    {
        string[] folders = { "Assets/BookPages", "Assets/Resources/GameSprites" };
        int n = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", folders);
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.maxTextureSize = 1024;                 // uzun tomon 1024 gacha
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.crunchedCompression = true;            // yuklab olish hajmini keskin kamaytiradi
                imp.compressionQuality = 50;
                imp.SaveAndReimport();
                n++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }

        Debug.Log($"[Book] {n} ta rasm web uchun optimallashtirildi.");
        EditorUtility.DisplayDialog("Tayyor",
            $"{n} ta rasm optimallashtirildi (1024px, crunch siqish).\nEndi WebGL'ni qayta build qiling — hajm va yuklanish ancha tezlashadi.", "OK");
    }

    [MenuItem("Tools/Book/Enable Drawing")]
    public static void EnableDrawing()
    {
        BookPro book = Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        if (book == null)
        {
            EditorUtility.DisplayDialog("Xato", "Ochiq sahnada BookPro topilmadi. Menu sahnasini oching.", "OK");
            return;
        }
        Canvas canvas = book.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault();

        if (Object.FindObjectsByType<BookDrawing>(FindObjectsSortMode.None).FirstOrDefault() == null)
        {
            var go = new GameObject("BookDrawing");
            Undo.RegisterCreatedObjectUndo(go, "Create BookDrawing");
            if (canvas != null) go.transform.SetParent(canvas.transform, false);
            go.AddComponent<BookDrawing>();
        }

        EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
        EditorSceneManager.SaveScene(book.gameObject.scene);
        EditorUtility.DisplayDialog("Tayyor",
            "Chizish yoqildi.\nO'yinda yuqoridagi 'CHIZISH' tugmasini bosib, sahifaga rangli qalam bilan chizasiz/bo'yaysiz.", "OK");
    }

    [MenuItem("Tools/Book/Add Games To Pages")]
    public static void AddGames()
    {
        BookPro book = Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        if (book == null)
        {
            EditorUtility.DisplayDialog("Xato", "Ochiq sahnada BookPro topilmadi. Menu sahnasini oching.", "OK");
            return;
        }

        Canvas canvas = book.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Xato", "Sahnada Canvas topilmadi.", "OK");
            return;
        }

        // 1) Sahifalardagi eski o'yin tugmalarini o'chiramiz (varaqlashga xalaqit berardi)
        int removed = 0;
        var oldButtons = Object.FindObjectsByType<PageGameButton>(FindObjectsSortMode.None);
        foreach (var b in oldButtons)
        {
            if (b != null && b.gameObject != null)
            {
                Undo.DestroyObjectImmediate(b.gameObject);
                removed++;
            }
        }

        // 2) Kontroller (doimiy "O'YIN" tugmasini o'zi yaratadi)
        var ctrl = Object.FindObjectsByType<BookGameController>(FindObjectsSortMode.None).FirstOrDefault();
        if (ctrl == null)
        {
            var go = new GameObject("BookGameController");
            Undo.RegisterCreatedObjectUndo(go, "Create BookGameController");
            go.transform.SetParent(canvas.transform, false);
            ctrl = go.AddComponent<BookGameController>();
        }

        EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
        EditorSceneManager.SaveScene(book.gameObject.scene);

        Debug.Log($"[BookGamesInstaller] Eski tugmalar o'chirildi: {removed}. Kontroller tayyor.");
        EditorUtility.DisplayDialog("Tayyor",
            $"O'yin tizimi o'rnatildi.\nSahifalardagi eski tugmalar o'chirildi: {removed}.\n" +
            "Endi ekranning yuqori-o'ng burchagida bitta 'O‘YIN' tugmasi chiqadi — u joriy sahifaning o'yinini ochadi.\nSahna saqlandi.", "OK");
    }
}
