using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.XR.ARKit;
#endif // UNITY_IOS && !UNITY_EDITOR

public class Float : MonoBehaviour
{
    // User Inputs
    public float degreesPerSecond = 15.0f;
    public float amplitude = 0.4f;
    public float frequency = 1f;
    public float height = 0.0f;
    ARCameraManager m_CameraManager;

    // Position Storage Variables
    Vector3 posOffset = new Vector3();
    Vector3 tempPos = new Vector3();
    Vector3 oldOffset = new Vector3();
    float oldHeight = GlobalAR.waterLevel;
    bool check = false;

    // Use this for initialization
    void Start()
    {
        // Store the starting position & rotation of the object
        transform.position = new Vector3(getRandomX(), oldHeight + height, getRandomZ());
        transform.rotation = new Quaternion(Random.Range(0f, 90.0f), Random.Range(0f, 90.0f), Random.Range(0f, 90.0f),0.0f);
        posOffset = transform.position;
        oldOffset = transform.position;
    }
    // Update is called once per frame
    void Awake()
    {
        m_CameraManager = FindObjectOfType<ARCameraManager>();
    }
    void Update()
    {
        if (oldHeight != GlobalAR.waterLevel)
        {
            oldHeight = GlobalAR.waterLevel;
            transform.position = new Vector3(transform.position.x, oldHeight + height, transform.position.z);
            posOffset = new Vector3(posOffset.x, oldHeight + height, posOffset.z);
            oldOffset = new Vector3(oldOffset.x, oldHeight + height, oldOffset.z);
        }
        posOffset.x = transform.position.x;
        // Spin object around Y-Axis
        transform.Rotate(new Vector3(0f, Time.deltaTime * degreesPerSecond, 0f), Space.World);

        // Float up/down with a Sin()
        tempPos = posOffset;
        tempPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude;

        transform.position = tempPos;
        if (transform.position.x > 8 + m_CameraManager.transform.position.x || transform.position.x < -8 + m_CameraManager.transform.position.x)
        {
            oldOffset = new Vector3(getRandomX(), oldHeight + height, getRandomZ());
            transform.position = oldOffset;
            posOffset = oldOffset;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (check)
        {
            StartCoroutine(Delay());
        }
        
    }
    private IEnumerator Delay()
    {
        check = false;
        yield return new WaitForSeconds(5);
        check = true;
        oldOffset = new Vector3(getRandomX(), oldHeight + height, getRandomZ());
        transform.position = oldOffset;
        posOffset = oldOffset;
    }
    private float getRandomX()
    {
        return (Random.Range(-7.0f, 7.0f) + m_CameraManager.transform.position.x);
    }
    private float getRandomZ()
    {
        return (Random.Range(-7.0f, 7.0f) + m_CameraManager.transform.position.z);
    }
}
