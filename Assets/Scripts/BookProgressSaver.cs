using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Kitobning joriy betini eslab qoladi: foydalanuvchi nechanchi betda chiqib ketsa,
/// qaytib kirganda o'sha betdan ochiladi. BookPro logikasiga TEGMAYDI — faqat
/// currentPaper ni PlayerPrefs ga saqlaydi/tiklaydi va OnFlip ga listener qo'shadi.
/// BookPro bilan bir xil GameObject ga tushadi.
/// </summary>
[RequireComponent(typeof(BookPro))]
public class BookProgressSaver : MonoBehaviour
{
    [Tooltip("Bo'sh bo'lsa sahna nomidan avtomatik yasaladi")]
    public string saveKey = "";

    BookPro book;

    void Awake()
    {
        book = GetComponent<BookPro>();
        if (string.IsNullOrEmpty(saveKey))
            saveKey = "BookLastPage_" + gameObject.scene.name;
    }

    IEnumerator Start()
    {
        // BookPro.Start() (UpdatePages / CalcCurlCriticalPoints) to'liq ishlashini kutamiz
        yield return null;

        int saved = PlayerPrefs.GetInt(saveKey, 0);
        // CurrentPaper setter'i qiymatni StartFlippingPaper..EndFlippingPaper+1 oralig'iga clamp qiladi
        book.CurrentPaper = saved;

        if (book.OnFlip == null)
            book.OnFlip = new UnityEvent();
        book.OnFlip.AddListener(Save);
    }

    /// <summary>Har varaqlaganda chaqiriladi (OnFlip)</summary>
    public void Save()
    {
        if (book == null) return;
        PlayerPrefs.SetInt(saveKey, book.CurrentPaper);
        PlayerPrefs.Save();
    }

    void OnDisable()
    {
        if (book != null && book.OnFlip != null)
            book.OnFlip.RemoveListener(Save);
        Save();
    }

    void OnApplicationQuit()
    {
        Save();
    }
}
