using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GISInfoView : MonoBehaviour
{
    public Text text;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 120 != 0) { return; }
        text.text = "経度：" + GlobalAR.currentlocation.Longitude + " 緯度："+ GlobalAR.currentlocation.Latitude;
    }
}
