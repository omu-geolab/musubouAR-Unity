using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARAnimationViewDemoController : MonoBehaviour
{
    // Start is called before the first frame update
    public Sprite[] sprites;

    //Private objects/variables
    private Image buttonImage;
    private int spriteIndex;
    void Start()
    {
        buttonImage = GetComponent<Image>(); // Get the image component of your button
        spriteIndex = 0; // Set the index to 0
        buttonImage.sprite = sprites[spriteIndex]; // Set the image to the first image in your sprite array
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    spriteIndex++; // Increment the index
        //    if (spriteIndex > sprites.Length - 1) // Ensure that you stay within the array size
        //    {
        //        spriteIndex = 0;
        //    }
        //    GlobalAR.demoMode = (GlobalAR.AnimationMode)spriteIndex;
        //    buttonImage.sprite = sprites[spriteIndex]; // Set the image
        //}
    }
    public void OnPressedDemo()
    {

        spriteIndex++; // Increment the index
        if (spriteIndex > sprites.Length - 1) // Ensure that you stay within the array size
        {
            spriteIndex = 0;
        }
        GlobalAR.demoMode = (GlobalAR.AnimationMode)spriteIndex;
        buttonImage.sprite = sprites[spriteIndex]; // Set the image
    }
}
