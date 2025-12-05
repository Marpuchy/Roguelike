using System.Collections;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private SpriteRenderer m_PlayerSprite;
    
    void Start()
    {
     m_PlayerSprite = GetComponent<SpriteRenderer>();
    }

    public IEnumerator ChangeColorAction()
    {
        m_PlayerSprite.color = Color.red ;
        yield return new WaitForSeconds(0.25f);
        m_PlayerSprite.color = Color.white;
    }


    public void ChangeColor()
    {
        StartCoroutine(ChangeColorAction());
    }
    
    
    
    
}
