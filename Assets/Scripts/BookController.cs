using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookController : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject BookPro;
    public GameObject HotSpot;
    public TMP_InputField PagesNomer;
    private float TextSize;
    private float Pos;
    RectTransform m_RectTransform;
    float m_XAxis, m_YAxis;
    private float Width;
    private float Height;
    private float TextWidth;
    private float TextHeight;
    void Start()
    {
        UIController();
        TextSizeController();
    }
    void UIController()
    {
        Width = Canvas.GetComponent<RectTransform>().rect.width;
        Height = Canvas.GetComponent<RectTransform>().rect.height;
        m_RectTransform = BookPro.GetComponent<RectTransform>();
        BookPro.GetComponent<RectTransform>().anchoredPosition = new Vector2(-Width / 2, 50);
        BookPro.GetComponent<RectTransform>().sizeDelta = new Vector2(Width * 2, Height - 600);
        HotSpot.GetComponent<RectTransform>().anchoredPosition = new Vector3(Width, 0, 0);
        HotSpot.GetComponent<RectTransform>().right = new Vector2(-Width, 0);
        HotSpot.GetComponent<RectTransform>().localRotation = Quaternion.EulerAngles(0, 0, 0);
    }
    public void Mun(int number)
    {
        BookPro.GetComponent<BookPro>().currentPaper = number;
        BookPro.GetComponent<BookPro>().UpdatePages();
    }
    public void AutoFlip()
    {
        int number = int.Parse(PagesNomer.text);
        BookPro.GetComponent<BookPro>().currentPaper = number;
    }
    public void UpdateAutoFlip()
    {
        BookPro.GetComponent<BookPro>().UpdatePages();
    }
    void TextSizeController()
    {
        //if (Width < 720)
        //{
        //    TextSize = 20;
        //    TextWidth = Width - 50;
        //    TextHeight = Height - 50;
        //}
        //else if (Width >= 720 && Width < 900)
        //{
        //    TextSize = 25;
        //    TextWidth = Width - 100;
        //    TextHeight = Height - 100;
        //}
        //else if (Width >= 900 && Width < 1100)
        //{
        //    TextSize = 35;
        //    TextWidth = Width - 150;
        //    TextHeight = Height - 150;
        //}
        //else if (Width >= 1100 && Width < 1300)
        //{
        //    TextSize = 40;
        //    TextWidth = Width - 200;
        //    TextHeight = Height - 200;
        //}
        //else if (Width >= 1300 && Width < 1500)
        //{
        //    TextSize = 45;
        //}
        //else if (Width >= 1500 && Width < 1700)
        //{
        //    TextSize = 50;
        //}
        //else if (Width >= 1700)
        //{
        //    TextSize = 55;
        //    TextWidth = Width - 400;
        //    TextHeight = Height - 350;
        //}
        //for (int i = 1; i < BookPro.GetComponent<BookPro>().papers.Length; i++)
        //{
        //    GameObject pages = BookPro.GetComponent<BookPro>().papers[i].Front;
        //    Pages.Add(pages);
        //}
        //for (int i = 1; i < Pages.Count; i++)
        //{
        //    Pages[0].transform.GetChild(0).GetComponent<GridLayoutGroup>().cellSize = new Vector2(Width - 50, 50);
        //    Pages[i].transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(TextWidth, TextHeight);
        //    Pages[i].transform.GetChild(0).GetComponent<TMP_Text>().fontSize = TextSize;
        //}
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
