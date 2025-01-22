using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BookUI : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject BookPro;
    public GameObject Left;
    private float Width;
    private float Height;
    public List<GameObject> Pages;
    private void Awake()
    {
        Width = Canvas.GetComponent<RectTransform>().rect.width;
        Height = Canvas.GetComponent<RectTransform>().rect.height;
        BookPro.GetComponent<RectTransform>().sizeDelta = new Vector2(0, Height / 2.3f);
        BookPro.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        BookPro.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        BookPro.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
    }
    
    void Start()
    {

        

    }
    void TextSize()
    {
        for (int i = 0; i < gameObject.GetComponent<BookPro>().papers.Length; i++)
        {
            
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
