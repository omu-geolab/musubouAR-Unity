using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }  
    public void OnButtonPressed()
    {
    	  Application.Unload();
    }
    public void OnQuit()
    {
    	  Application.Quit();
    }

}
