using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Kitobning MAVJUD logikasiga (BookPro / BookController) tegmasdan sahifalarni to'ldiradi.
/// DIZAYN: Front tomon = PDF sahifalari (Assets/BookPages), Back tomon = bitta oddiy sahifa.
/// Har PDF sahifasi bitta paperning FRONT tomoniga tushadi -> 112 sahifa = 112 paper.
///
/// Muhim: template (RightPage/LeftPage) NUSXALANMAYDI (ularda child'lar bor, bloat qiladi).
/// Buning o'rniga noldan toza UI sahifa yaratiladi va faqat RectTransform qiymatlari ko'chiriladi.
/// </summary>
public static class BookPdfFiller
{
    const string PagesFolder = "Assets/BookPages";
    // Oddiy (bo'sh) sahifa sprite guid'i — asl kitobning back tomoni
    const string PlainPageGuid = "15586bb96b34a174daaeca4e1d8fe2f7";

    [MenuItem("Tools/Book/Fill Book From BookPages")]
    public static void Fill()
    {
        BookPro book = Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        if (book == null)
        {
            EditorUtility.DisplayDialog("Xato", "Ochiq sahnada BookPro topilmadi. Menu sahnasini oching.", "OK");
            return;
        }

        var pageSprites = LoadPdfSprites();
        if (pageSprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Xato", PagesFolder + " ichida rasm topilmadi.", "OK");
            return;
        }

        Sprite plain = LoadPlainSprite();

        Undo.RegisterCompleteObjectUndo(book, "Fill Book");

        // 1) Eski sahifalarni (mavjud papers ichidagi GameObject'larni) o'chiramiz
        if (book.papers != null)
        {
            foreach (var p in book.papers)
            {
                if (p == null) continue;
                if (p.Front != null) Undo.DestroyObjectImmediate(p.Front);
                if (p.Back != null) Undo.DestroyObjectImmediate(p.Back);
            }
        }

        // 2) Har PDF sahifasi uchun bitta paper: Front = sahifa, Back = oddiy sahifa
        var papers = new List<Paper>(pageSprites.Count);
        for (int i = 0; i < pageSprites.Count; i++)
        {
            GameObject front = NewPage(book, book.RightPageTransform, "Page" + (i * 2), pageSprites[i]);
            GameObject back  = NewPage(book, book.LeftPageTransform,  "Page" + (i * 2 + 1), plain);
            papers.Add(new Paper { Front = front, Back = back });
        }

        book.papers = papers.ToArray();
        book.StartFlippingPaper = 0;
        book.EndFlippingPaper = book.papers.Length - 1;
        book.currentPaper = 0;
        book.UpdatePages();

        EditorUtility.SetDirty(book);
        EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
        EditorSceneManager.SaveScene(book.gameObject.scene);

        Debug.Log($"[BookPdfFiller] Tayyor: {pageSprites.Count} sahifa (Front), Back = oddiy sahifa. Papers: {book.papers.Length}.");
        EditorUtility.DisplayDialog("Tayyor",
            $"{pageSprites.Count} PDF sahifasi Front tomonga joylandi.\nBack tomon = oddiy sahifa.\nPapers: {book.papers.Length}.\nSahna saqlandi.", "OK");
    }

    [MenuItem("Tools/Book/Enable Page Memory")]
    public static void EnablePageMemory()
    {
        BookPro book = Object.FindObjectsByType<BookPro>(FindObjectsSortMode.None).FirstOrDefault();
        if (book == null)
        {
            EditorUtility.DisplayDialog("Xato", "Ochiq sahnada BookPro topilmadi. Menu sahnasini oching.", "OK");
            return;
        }

        var saver = book.GetComponent<BookProgressSaver>();
        if (saver == null)
            saver = Undo.AddComponent<BookProgressSaver>(book.gameObject);

        EditorUtility.SetDirty(book.gameObject);
        EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
        EditorSceneManager.SaveScene(book.gameObject.scene);

        EditorUtility.DisplayDialog("Tayyor",
            "Bet xotirasi yoqildi.\nKitob nechanchi betda yopilsa, qaytib kirganda o'sha betdan ochiladi.", "OK");
    }

    // Noldan toza UI sahifa yaratish (template NUSXALANMAYDI)
    static GameObject NewPage(BookPro book, RectTransform template, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                                typeof(Image), typeof(Mask), typeof(CanvasGroup));
        go.layer = book.gameObject.layer; // UI (5)

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(book.transform, false);
        if (template != null)
        {
            rt.anchorMin = template.anchorMin;
            rt.anchorMax = template.anchorMax;
            rt.pivot = template.pivot;
            rt.sizeDelta = template.sizeDelta;
            rt.anchoredPosition = template.anchoredPosition;
            rt.localScale = template.localScale;
        }
        rt.localRotation = Quaternion.identity;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = true;

        go.GetComponent<Mask>().showMaskGraphic = true;

        Undo.RegisterCreatedObjectUndo(go, "Create Book Page");
        return go;
    }

    static List<Sprite> LoadPdfSprites()
    {
        var result = new List<Sprite>();
        if (!Directory.Exists(PagesFolder)) return result;

        var files = Directory.GetFiles(PagesFolder, "*.png")
                             .Concat(Directory.GetFiles(PagesFolder, "*.jpg"))
                             .OrderBy(f => f, System.StringComparer.Ordinal)
                             .ToList();

        foreach (var f in files)
        {
            string assetPath = f.Replace('\\', '/');
            EnsureSprite(assetPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) result.Add(sprite);
        }
        return result;
    }

    static Sprite LoadPlainSprite()
    {
        string path = AssetDatabase.GUIDToAssetPath(PlainPageGuid);
        if (string.IsNullOrEmpty(path)) return null;
        EnsureSprite(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
