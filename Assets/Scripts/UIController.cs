using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public List<GameObject> ceils;
    public GameObject Canvas;
    public float Width;
    public float Height;
    private void Awake()
    {
        Width = Canvas.GetComponent<RectTransform>().rect.width - 100;
        Height = Canvas.GetComponent<RectTransform>().rect.height;
        for (int i = 0; i < ceils.Count; i++)
        {
            ceils[i].GetComponent<RectTransform>().sizeDelta = new Vector2( Width, ceils[0].GetComponent<RectTransform>().rect.height); ;
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
