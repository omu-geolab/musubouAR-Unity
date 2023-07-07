using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.XR.ARKit;
#endif // UNITY_IOS && !UNITY_EDITOR
public class BoxVolumeController : MonoBehaviour
{

    ARCameraManager m_CameraManager;
    // Start is called before the first frame update
    public float heigth = 0.8f;
    public float maxHeigth = 2.0f;
    public float minHeigth = 0.2f;
    public float offset = 0.2f;
    float adjust = 0.0f;

    ARPlaneManager m_ARPlaneManager;
    bool flag = false;

    void Start()
    {

    }
    void Awake()
    {
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        m_CameraManager = FindObjectOfType<ARCameraManager>();
    }
    private void OnEnable()
    {
        heigth = 0.8f;
        maxHeigth = 2.0f;
        minHeigth = 0.2f;
        offset = 0.2f;
        adjust = 0.0f;
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        m_CameraManager = FindObjectOfType<ARCameraManager>();
        flag = false;

    }

    // Update is called once per frame
    void Update()
    {
        // transform.position = new Vector3(0.0f, height, 0.0f);
        if (!flag)
        {
            foreach (var plane in m_ARPlaneManager.trackables)
            {
                if (plane.classification == PlaneClassification.Floor)
                {
                    adjust = plane.center.y;
                    heigth = offset * 4.0f;
                    flag = true;
                    break;
                }
            }
        }
        transform.position = new Vector3(m_CameraManager.transform.position.x, heigth + adjust, m_CameraManager.transform.position.z);
    }

    public void MoveUp()
    {
        if (heigth < maxHeigth)
        {
            heigth = heigth + offset;
            transform.Translate(0.0f, offset, 0.0f);
        }

    }
    public void MoveDown()
    {
        if (heigth > minHeigth)
        {
            heigth = heigth - offset;
            transform.Translate(0.0f, offset, 0.0f);
        }
    }
}
