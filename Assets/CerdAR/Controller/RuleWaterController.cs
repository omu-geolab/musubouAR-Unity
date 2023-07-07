using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.XR.ARKit;
#endif // UNITY_IOS && !UNITY_EDITOR

public class RuleWaterController : MonoBehaviour
{
    public float heigth = 1.25f;
    ARPlaneManager m_ARPlaneManager;
    bool flag = false;
    void Start()
    {

    }
    void Awake()
    {
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
    }
    private void OnEnable()
    {
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        flag = false;
    }

    // Update is called once per frame
    void Update()
    {
        //if (!flag)
        //{
        if (flag)
        {
            if (Time.frameCount % 100 == 0) { return; }
        }
        foreach (var plane in m_ARPlaneManager.trackables)
        {
            if (plane.classification == PlaneClassification.Floor)
            {
                Debug.Log("Floor");
                float adjust = plane.center.y + heigth;
                transform.position = new Vector3(plane.center.x, adjust, plane.center.z);
                flag = true;
                break;
            }
        }
        //}

    }
}
