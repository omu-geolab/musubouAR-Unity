using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.XR.ARKit;
#endif // UNITY_IOS && !UNITY_EDITOR

public class liquefaction : MonoBehaviour
{
    ARCameraManager m_CameraManager;
    ARPlaneManager m_ARPlaneManager;
    bool flag = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    void Awake()
    {
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        m_CameraManager = FindObjectOfType<ARCameraManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!flag)
        {
            foreach (var plane in m_ARPlaneManager.trackables)
            {
                if (plane.classification == PlaneClassification.Floor)
                {
                    flag = true;
                    transform.position = new Vector3(m_CameraManager.transform.position.x, plane.center.y + 1, m_CameraManager.transform.position.z);
                    break;
                }
            }
        }
    }
    public void MoveUp()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + (float)0.1, transform.position.z);

    }
    public void MoveDown()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y - (float)0.1, transform.position.z);
    }
}
