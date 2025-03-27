using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using JSONModel;
using Newtonsoft.Json;
using System.Device.Location;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARKit;
using System.IO;

/// <summary>
/// Main controller for the AR application
/// Refactored for better organization, performance, and maintainability
/// </summary>
public class MainController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogInfo;
    public GameObject notice;

    [Header("Disaster Effects")]
    public GameObject waterAction;
    public GameObject liqueAction;

    [Header("AR Components")]
    public AROcclusionManager occlusionManager;
    public ARMeshManager meshManager;
    public MeshFilter nonePrefab;
    public MeshFilter occPrefab;

    [Header("Audio")]
    public AudioSource houseAudio;
    public AudioSource rockFallAudio;

    // Private references to AR components
    private ARPlaneManager arPlaneManager;
    private ARCameraManager arCameraManager;
    private ARSessionOrigin arSessionOrigin;
    private ARSession arSession;

    // Resource managers
    private DisasterEffectsManager disasterEffects;
    private UIManager uiManager;
    private ResourceManager resourceManager;

    // State tracking
    private bool isFirstRun = true;
    private float noticeUpdateInterval = 3f;

    // Initialization
    private void Awake()
    {
        // Get AR components references
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arCameraManager = FindObjectOfType<ARCameraManager>();
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        arSession = FindObjectOfType<ARSession>();

        // Initialize managers
        resourceManager = new ResourceManager();
        uiManager = new UIManager(dialogInfo, notice);
        disasterEffects = new DisasterEffectsManager(
            arPlaneManager,
            arCameraManager,
            occlusionManager,
            meshManager,
            nonePrefab,
            occPrefab,
            waterAction,
            liqueAction,
            houseAudio,
            rockFallAudio
        );
    }

    private void Start()
    {
        Debug.Log("Starting application");
        InitializeUI();
        LoadGeoData();
        InvokeRepeating(nameof(UpdateNotice), 0.1f, noticeUpdateInterval);
    }

    private void OnEnable()
    {
        ResetAR();
        GlobalAR.currentWarn = "";
    }

    private void InitializeUI()
    {
        uiManager.HideAll();
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
        meshManager.meshPrefab = nonePrefab;
    }

    private void LoadGeoData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "data.geojson");
        Debug.Log($"Checking for data at: {filePath}");

        if (File.Exists(filePath))
        {
            Debug.Log("Loading data from persistent path");
            string jsonData = File.ReadAllText(filePath);
            ProcessJsonData(jsonData);
        }
        else
        {
            Debug.Log("Downloading data from server");
            StartCoroutine(DownloadGeoData());
        }
    }

    private IEnumerator DownloadGeoData()
    {
        string url = GetDisasterLink();
        if (string.IsNullOrEmpty(url))
        {
            url = "https://raw.githubusercontent.com/th-nguyen/cerdar-asset/refs/heads/main/data.geojson";
            //Debug.LogError("No disaster link available");
            //yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading data: {www.error}");
            }
            else
            {
                string jsonData = www.downloadHandler.text;
                ProcessJsonData(jsonData);

                // Cache the data for future use
                File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, "data.geojson"),
                    jsonData
                );
            }
        }
    }

    private void ProcessJsonData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("Received empty JSON data");
            return;
        }

        try
        {
            Root geoData = JsonConvert.DeserializeObject<Root>(jsonData);
            GlobalAR.dataManager.storeData(geoData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON data: {ex.Message}");
        }
    }

    private string GetDisasterLink()
    {
        string jsonFilePath = Path.Combine(Application.persistentDataPath, "disaster.json");

        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError("Disaster configuration file not found");
            return string.Empty;
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            Disaster disasterConfig = JsonUtility.FromJson<Disaster>(jsonContent);
            Debug.Log($"Using disaster link: {disasterConfig.disaster_link}");
            return disasterConfig.disaster_link;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading disaster configuration: {e.Message}");
            return string.Empty;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        HandleTouchInput();

        if (!GlobalAR.isWorldLoad) return;

        // Only perform these checks every 30 frames for performance
        if (Time.frameCount % 30 != 0) return;

        if (isFirstRun)
        {
            isFirstRun = false;
            Initialize3DView();
        }

        UpdateARObjects();
        HandleDialogDisplay();
        UpdateDisasterEffects();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount <= 0) return;

        Touch touch = Input.touches[0];
        if (touch.phase != TouchPhase.Began) return;

        Ray ray = arSessionOrigin.camera.ScreenPointToRay(touch.position);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;

            GameObject touchedObject = hit.transform.gameObject;
            InfoModelController info = touchedObject.GetComponent<InfoModelController>();

            if (info != null)
            {
                GlobalAR.currentFeature = info.feature;
                GlobalAR.isViewDialog = true;
                break;
            }
        }
    }

    private void Initialize3DView()
    {
        DisplayInfoObjects(GlobalAR.dataManager.infoBox);
        DisplayInfoObjects(GlobalAR.dataManager.warnBox);
    }

    private void UpdateARObjects()
    {
        DisplayInfoObjects(GlobalAR.outsideAR);
    }

    private void HandleDialogDisplay()
    {
        if (GlobalAR.isViewDialog && !GlobalAR.isShowedDialog)
        {
            uiManager.ShowDialog();
        }
    }

    private void UpdateDisasterEffects()
    {
        if (GlobalAR.insideWarn.Count > 0 && GlobalAR.demoMode == GlobalAR.AnimationMode.None)
        {
            var feature = GlobalAR.insideWarn[0];
            disasterEffects.ApplyDisasterEffect(feature);
        }
        else
        {
            // Handle demo mode or no active warnings
            if (GlobalAR.demoMode == GlobalAR.AnimationMode.None)
            {
                disasterEffects.ClearAllEffects();
            }
            else
            {
                disasterEffects.ApplyDemoEffect(GlobalAR.demoMode);
            }
        }
    }

    private void UpdateNotice()
    {
        uiManager.UpdateNoticeMessages(GlobalAR.insideWarn, GlobalAR.nearWarn);
    }

    private void DisplayInfoObjects(List<JSONModel.Feature> features)
    {
        if (features == null || features.Count == 0) return;

        List<JSONModel.Feature> processed = new List<JSONModel.Feature>();

        foreach (JSONModel.Feature feature in features)
        {
            GeoCoordinate targetLocation = new GeoCoordinate(
                feature.geometry.coordinates[1],
                feature.geometry.coordinates[0]
            );

            float distance = (float)targetLocation.GetDistanceTo(GlobalAR.currentlocation);

            // Adjust distance for warning features with a range
            if (feature.properties.info_type == GlobalAR.kWarn && feature.properties.range != null)
            {
                distance -= (float)feature.properties.range;
            }

            if (distance > GlobalAR.disCam)
            {
                if (!GlobalAR.outsideAR.Contains(feature))
                {
                    GlobalAR.outsideAR.Add(feature);
                }
            }
            else if (!GlobalAR.insideAR.Contains(feature))
            {
                SpawnInfoObject(feature, targetLocation);
                GlobalAR.insideAR.Add(feature);
                processed.Add(feature);
            }
        }

        // Remove processed features from the outside list
        foreach (var feature in processed)
        {
            if (GlobalAR.outsideAR.Contains(feature))
            {
                GlobalAR.outsideAR.Remove(feature);
            }
        }
    }

    private void SpawnInfoObject(JSONModel.Feature feature, GeoCoordinate targetLocation)
    {
        string prefabName = DeterminePrefabName(feature);
        GameObject prefab = resourceManager.GetPrefab(prefabName);

        if (prefab == null) return;

        Vector3 position = GeoTool.ConvertGPS2Position(
            targetLocation,
            GlobalAR.heightAR,
            arCameraManager.transform.position
        );

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        instance.tag = "ARViewer";

        InfoModelController infoController = instance.GetComponent<InfoModelController>();
        if (infoController != null)
        {
            infoController.feature = feature;
        }
    }

    private string DeterminePrefabName(JSONModel.Feature feature)
    {
        if (!string.IsNullOrEmpty(feature.properties.icon))
        {
            return feature.properties.icon.Replace(".png", "").Replace(".jpg", "");
        }

        return feature.properties.info_type == GlobalAR.kWarn ? "icon_warn3" : "icon_infoTag";
    }

    public void ResetAR()
    {
        // Destroy all AR objects
        GameObject[] arObjects = GameObject.FindGameObjectsWithTag("ARViewer");
        foreach (GameObject obj in arObjects)
        {
            Destroy(obj);
        }

        // Re-acquire AR components
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arCameraManager = FindObjectOfType<ARCameraManager>();
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        arSession = FindObjectOfType<ARSession>();

        // Reset all state
        disasterEffects.Reset();
        uiManager.Reset();

        // Reset global state
        GlobalAR.insideWarn.Clear();
        GlobalAR.nearWarn.Clear();
        GlobalAR.insideAR.Clear();
        GlobalAR.outsideAR.Clear();
        GlobalAR.isWorldLoad = false;
        isFirstRun = true;

        // Reset AR session on iOS
#if UNITY_IOS
        var sessionSubsystem = (ARKitSessionSubsystem)arSession.subsystem;
        if (sessionSubsystem != null)
        {
            arSession.Reset();
        }
#endif
    }
}