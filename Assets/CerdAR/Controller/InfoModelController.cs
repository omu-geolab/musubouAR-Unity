using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONModel;
using System.Device.Location;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using System.Linq;

public class InfoModelController : MonoBehaviour, IPointerClickHandler
{
    // Feature data
    public Feature feature;

    // Components
    Camera m_CameraManager;
    public TextMesh distanceText;
#nullable enable
    public TextMesh? description;

    // Update interval control
    private float updateInterval = 0.2f;
    private float nextUpdateTime = 0;

    void Start()
    {
        m_CameraManager = GameObject.Find("AR Camera").GetComponent<Camera>();
        gameObject.tag = "ARViewer";

        // Initialize position on start
        if (feature != null)
        {
            UpdatePositionAndRotation();
        }
    }

    private void OnEnable()
    {
        m_CameraManager = GameObject.Find("AR Camera").GetComponent<Camera>();
    }

    void Update()
    {
        if (feature == null) return;

        // Update at fixed intervals
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdatePositionAndRotation();
        }
    }

    private void UpdatePositionAndRotation()
    {
        if (feature == null) return;

        GeoCoordinate target = new GeoCoordinate(feature.geometry.coordinates[1], feature.geometry.coordinates[0]);

        // Calculate distance
        var distanceGPS = target.GetDistanceTo(GlobalAR.currentlocation);
        var distance = distanceGPS;

        // Process warnings
        if (feature.properties.info_type == GlobalAR.kWarn)
        {
            if (feature.properties.range != null)
            {
                var range = (float)feature.properties.range;
                distance = distance - range;

                if (distance <= 0)
                {
                    distance = 0;
                    if (!GlobalAR.insideWarn.Contains(feature))
                    {
                        GlobalAR.insideWarn.Add(feature);
                    }
                    GlobalAR.nearWarn = GlobalAR.nearWarn.Where(item => item.id != feature.id).ToList();
                }
                else
                {
                    if (distance < 5)
                    {
                        if (!GlobalAR.nearWarn.Contains(feature))
                        {
                            GlobalAR.nearWarn.Add(feature);
                        }
                    }
                    else
                    {
                        GlobalAR.nearWarn = GlobalAR.nearWarn.Where(item => item.id != feature.id).ToList();
                    }
                    GlobalAR.insideWarn = GlobalAR.insideWarn.Where(item => item.id != feature.id).ToList();
                }
                distanceText.text = distance.ToString("F0") + "m";
            }
        }
        else
        {
            distanceText.text = distance.ToString("F0") + "m";
        }

        // Check if in range
        if (distance > GlobalAR.disCam)
        {
            GlobalAR.outsideAR.Add(feature);
            GlobalAR.insideAR = GlobalAR.insideAR.Where(item => item.id != feature.id).ToList();
            Destroy(gameObject);
            return;
        }

        // Position using the original GPS conversion method
        Vector3 position = ConvertGPS2ARCoordinate(feature);

        // Apply position offset from camera
        Vector3 cameraPosXZ = new Vector3(m_CameraManager.transform.position.x, 0, m_CameraManager.transform.position.z);
        position = position + cameraPosXZ;

        // Set final position
        transform.position = new Vector3(position.x, GlobalAR.heightAR, position.z);

        // Simply copy camera's rotation directly to the icon
        // Try a very direct approach - just copy the camera's exact rotation
        // This forces icons to face exactly the same direction as the camera
        transform.rotation = Quaternion.Euler(0, m_CameraManager.transform.rotation.eulerAngles.y, 0);

        // Scale based on distance - with increased size
        float scale = Mathf.Clamp((float)(1.0 + 0.1 * distanceGPS), 1.0f, 7.0f);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    // The original coordinate conversion method from your code
    public Vector3 ConvertGPS2ARCoordinate(Feature ft)
    {
        double dz = (ft.geometry.coordinates[1] - GlobalAR.userLat) * GlobalAR.lat2km;   // -z is south
        double dx = -(ft.geometry.coordinates[0] - GlobalAR.userLng) * GlobalAR.lat2km; // +x is east
        return new Vector3((float)dx, 0, (float)dz);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (feature == null) return;

        GlobalAR.currentFeature = feature;
        GlobalAR.isViewDialog = true;
    }

    public void onClick()
    {
        if (feature == null) return;

        GlobalAR.currentFeature = feature;
        GlobalAR.isViewDialog = true;
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}