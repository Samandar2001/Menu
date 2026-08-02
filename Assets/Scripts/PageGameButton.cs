using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sahifadagi "O'yin" tugmasiga tushadi. Bosilganda o'sha sahifaga mos o'yinni ochadi.
/// </summary>
[RequireComponent(typeof(Button))]
public class PageGameButton : MonoBehaviour
{
    public int pageIndex;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Open);
    }

    void Open()
    {
        if (BookGameController.Instance != null)
            BookGameController.Instance.OpenForPage(pageIndex);
        else
            Debug.LogWarning("[PageGameButton] BookGameController sahnada yo'q.");
    }
}
