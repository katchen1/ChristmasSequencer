using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Float : MonoBehaviour
{
    private float y;
    private float direction = 1f; // 1 up, -1 down
    private float range = 0.035f;
    private float step = 0.0002f;

    // Start is called before the first frame update
    void Start()
    {
        y = gameObject.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        float newY = gameObject.transform.position.y + direction * step;
        if (newY > y + range)
        {
            newY = y + range;
            direction = -1f;
        }
        if (newY < y - range)
        {
            newY = y - range;
            direction = 1f;
        }
        gameObject.transform.position = new Vector3(
            gameObject.transform.position.x,
            newY,
            gameObject.transform.position.z
        );
    }

    public void SetY(float newY)
    {
        y = newY;
    }
    public float GetY()
    {
        return y;
    }
}
