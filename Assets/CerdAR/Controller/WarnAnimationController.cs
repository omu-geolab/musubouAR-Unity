using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class WarnAnimationController : MonoBehaviour
{
    // Start is called before the first frame update
    public string warnID = "";
    private float waitTime = 5.0f;
    private float timer = 0.0f;
    ARCameraManager m_CameraManager;
    void Start()
    {
        
    }
    private void Awake()
    {
        m_CameraManager = FindObjectOfType<ARCameraManager>();
    }
    private void OnEnable()
    {
        m_CameraManager = FindObjectOfType<ARCameraManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(GlobalAR.currentWarn != warnID)
        {
            Destroy(gameObject);
        }
        if (GlobalAR.currentWarn == "wall")
        {
            timer += Time.deltaTime;

            if (timer > waitTime)
            {
                var tranformOld = transform;
                Destroy(gameObject);
                GameObject obj = (GameObject)Resources.Load("Wall");
                obj.transform.position = tranformOld.position;
                obj.transform.rotation = tranformOld.rotation;
                Instantiate(obj);
                timer = 0.0f;

            }
        }
        //if (GlobalAR.currentWarn == "house")
        //{
        //    timer += Time.deltaTime;

        //    if (timer > waitTime)
        //    {
        //        Destroy(gameObject);
        //        m_CameraManager = FindObjectOfType<ARCameraManager>();
        //        GameObject obj = (GameObject)Resources.Load("HouseAnimation");
        //        var pos = m_CameraManager.transform.position;
        //        obj.transform.localPosition = new Vector3(pos.x - 5, pos.y - 2, pos.z + 15);
        //        obj.transform.LookAt(m_CameraManager.transform.forward);
        //        Instantiate(obj);

        //    }
        //}

    }
}
