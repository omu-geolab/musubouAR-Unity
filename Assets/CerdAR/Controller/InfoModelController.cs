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
    // Start is called before the first frame update
    public Feature feature;

    Camera m_CameraManager;
    public TextMesh distanceText;
    public TextMesh? description;

    void Start()
    {
        m_CameraManager = GameObject.Find("AR Camera").GetComponent<Camera>();
        gameObject.tag = "ARViewer";
        //if (description != null && feature != null)
        //{
        //    description.text = feature.properties.description;
        //}
    }

    private void OnEnable()
    {
        m_CameraManager = GameObject.Find("AR Camera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (feature == null) return;
        if (Time.frameCount % 60 != 0) { return; }

        GeoCoordinate target = new GeoCoordinate(feature.geometry.coordinates[1], feature.geometry.coordinates[0]);

        var distanceGPS = target.GetDistanceTo(GlobalAR.currentlocation);
        var distance = distanceGPS;
        //Vector3 position = new Vector3();

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

                    //position = ConvertGPS2ARCoordinate(feature);
                    //position.y = -4000;

                }
                else
                {
                    if (distance < 50)
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

                    //position = GeoTool.ConvertCoordinateFromCurrentLocation(target,GlobalAR.heightAR, 0);
                    //position = ConvertGPS2ARCoordinate(feature);
                }
                distanceText.text = distance.ToString("F0") + "m";

            }
        }
        else
        {
            //position = ConvertGPS2ARCoordinate(feature);
            distanceText.text = distance.ToString("F0") + "m";

        }

        if (distance > GlobalAR.disCam)
        {
            GlobalAR.outsideAR.Add(feature);
            GlobalAR.insideAR = GlobalAR.insideAR.Where(item => item.id != feature.id).ToList();
            Destroy(gameObject);
        }

        var bearing = GeoTool.CalculateBearing( GlobalAR.currentlocation,target);
        //float x = position.x + m_CameraManager.transform.position.x;
        //float y = position.y + m_CameraManager.transform.position.y;
        //float z = position.z + m_CameraManager.transform.position.z;
        //transform.position = new Vector3(x, y,z);
        //Debug.Log(position.ToString());
        //var angle = Mathf.Atan2(transform.position.z - m_CameraManager.transform.position.z , transform.position.x - m_CameraManager.transform.position.x);
        //float degrees = (180 / Mathf.PI) * angle;
        //transform.LookAt(m_CameraManager.transform.forward);
        //Debug.Log(degrees.ToString());
        //Debug.Log(bearing.ToString());
        transform.rotation =  Quaternion.Euler(0, 180 + (float)bearing, 0);
        float scale = (float)(0.5 + 0.1 * distanceGPS);
        transform.localScale = new Vector3(scale, scale, scale);




    }
    public Vector3 ConvertGPS2ARCoordinate(Feature ft)
    {
        double dz = (ft.geometry.coordinates[1] - GlobalAR.userLat) * GlobalAR.lat2km;   // -zÇ™ìÏï˚å¸
        double dx = -(ft.geometry.coordinates[0] - GlobalAR.userLng) * GlobalAR.lat2km; // +xÇ™ìåï˚å¸
        return new Vector3((float)dx, 0, (float)dz);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (feature == null) return;

        GlobalAR.currentFeature = feature;
        GlobalAR.isViewDialog = true;
        //DialogInfoController info = dialog.GetComponent<DialogInfoController>();
        //if (info == null) return;
        //info.setFeature(feature);

        //Debug.Log(feature.properties.photo);
        //Debug.Log(Application.persistentDataPath);
        //Debug.Log(Application.dataPath);
        //Debug.Log(Application.absoluteURL);
    }
    public void onClick()
    {
        if (feature == null) return;

        GlobalAR.currentFeature = feature;
        GlobalAR.isViewDialog = true;
        //DialogInfoController info = dialog.GetComponent<DialogInfoController>();
        //if (info == null) return;
        //info.setFeature(feature);

        //Debug.Log(feature.properties.photo);
        Debug.Log(Application.persistentDataPath);
        Debug.Log(Application.dataPath);
        Debug.Log(Application.absoluteURL);
    }
    private void OnDisable()
    {
        Destroy(gameObject);
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Ç∑ÇËî≤ÇØÇΩÅI");
    }

}
