using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swing : MonoBehaviour
{
    public float swingSpeed = 5f; 
    public float swingAmount = 1f;
    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        
        float swingValue = Mathf.Sin(Time.time * swingSpeed) * swingAmount;
        transform.position = initialPosition + new Vector3(0f, swingValue, 0f);
    }
}
