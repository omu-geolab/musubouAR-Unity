using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelDestroyController : MonoBehaviour
{
    // Start is called before the first frame update
    private float timer = 0.0f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer+=Time.deltaTime;
        if (timer > 8.0f)
        {
            Destroy(gameObject);
        }
    }
}
