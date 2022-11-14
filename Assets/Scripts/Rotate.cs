using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    private float m_Speed = 30.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float alpha = gameObject.GetComponent<SpriteRenderer>().color.a;
        if (alpha == 1f)
        {
            gameObject.transform.Rotate(new Vector3(0, 0, 1) * Time.deltaTime * m_Speed);
        }
    }
}
