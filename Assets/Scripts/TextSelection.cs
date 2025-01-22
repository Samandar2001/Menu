using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextSelection : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public TMP_Text tmpText; // TextMesh Pro matni
    private int startIndex = -1;
    private int endIndex = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Sichqoncha bosilganda boshlang'ich belgini aniqlaymiz
        startIndex = TMP_TextUtilities.FindIntersectingCharacter(tmpText, Input.mousePosition, null, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Sichqoncha qo'yib yuborilganda tugash belgini aniqlaymiz
        endIndex = TMP_TextUtilities.FindIntersectingCharacter(tmpText, Input.mousePosition, null, true);
        CopySelectedText(); // Tanlangan matnni nusxa olish
    }

    void CopySelectedText()
    {
        if (startIndex >= 0 && endIndex >= 0 && startIndex != endIndex)
        {
            int first = Mathf.Min(startIndex, endIndex);
            int last = Mathf.Max(startIndex, endIndex);

            string selectedText = tmpText.text.Substring(first, last - first + 1);
            GUIUtility.systemCopyBuffer = selectedText; // Klipbordga nusxa oladi
            Debug.Log("Nusxa olindi: " + selectedText);
        }
        else
        {
            Debug.LogWarning("Tanlangan matn noto‘g‘ri!");
        }
    }
}
