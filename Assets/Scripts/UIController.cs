using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public List<GameObject> ceils;
    public GameObject Canvas;
    List<Animator> animations;
    private float Width;
    private float Height;
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
        StartCoroutine(WaitAndPrint());
    }

    IEnumerator WaitAndPrint()
    {
        //ceils[0].transform.GetComponent<Animator>().Play("New Animation");
        //yield return new WaitForSeconds(2f);
        //ceils[1].transform.GetComponent<Animator>().Play("New Animation");
        //yield return new WaitForSeconds(2f);
        //ceils[2].transform.GetComponent<Animator>().Play("New Animation");
        //yield return new WaitForSeconds(2f);
        //ceils[3].transform.GetComponent<Animator>().Play("New Animation");
        //yield return new WaitForSeconds(2f);
        //ceils[4].transform.GetComponent<Animator>().Play("New Animation");
        yield return new WaitForSeconds(2f);
        //StartCoroutine(WaitAndPrint());

    }
    void Update()
    {

    }
}

