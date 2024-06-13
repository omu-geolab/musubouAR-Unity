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

public class MainController : MonoBehaviour
{
    // Start is called before the first frame update
    bool firstRun = true;
    public GameObject dialogInfo;
    public GameObject notice;
    public GameObject WaterAction;
    public GameObject LiqueAction;
    public AROcclusionManager occlusionManager;
    List<string> noticeMsg = new List<string>();
    List<string> planeAddFire = new List<string>();
    List<string> planeAddBlock = new List<string>();
    List<string> planeLiqueBlock = new List<string>();
    bool isHill = false;
    bool isHouse = false;
    bool isFire = false;
    bool isWater = false;
    bool isWall = false;
    bool isLique = false;
    ARPlaneManager m_ARPlaneManager;
    ARCameraManager m_CameraManager;
    ARSessionOrigin m_ARSessionOrigin;
    ARSession m_ARSession;
    public ARMeshManager meshManager;
    public MeshFilter nonePrefab;
    public MeshFilter occPrefab;
    public AudioSource houseAudio;
    public AudioSource rockFallAudio;

    RaycastHit _hit;

    void Start()
    {
        Debug.Log("start");
        dialogInfo.SetActive(false);
        notice.SetActive(false);
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
        meshManager.meshPrefab = nonePrefab;
        string text = "";
        string path = Path.Combine(Application.persistentDataPath, "data.geojson");
        Debug.Log(path);
        if (File.Exists(path))
        {
            Debug.Log("path");
            text = File.ReadAllText(path);
        }
        else
        {
            //Debug.Log("data");
            //var textResource = Resources.Load<TextAsset>("data");
            //text = textResource.text;
        }
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("download");
            StartCoroutine(GetText());
        }
        else
        {
            var myObject = JsonConvert.DeserializeObject<Root>(text);
            GlobalAR.dataManager.storeData(myObject);
        }

        InvokeRepeating("updateNotice", 0.1f, 3f);
    }
    void Awake()
    {
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        m_CameraManager = FindObjectOfType<ARCameraManager>();
        m_ARSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        m_ARSession = FindObjectOfType<ARSession>();
    }
    private void OnEnable()
    {
        resetAR();
        GlobalAR.currentWarn = "";
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            if (touch.phase == TouchPhase.Began)
            {
                var _ray = m_ARSessionOrigin.camera.ScreenPointToRay(touch.position);
                RaycastHit[] hit = Physics.RaycastAll(_ray);

                for (int i = 0; i < hit.Length; i++)
                {
                    if (hit[i].collider != null)
                    {
                        var tapObject = hit[i].transform.gameObject;

                        InfoModelController info = tapObject.GetComponent("InfoModelController") as InfoModelController;
                        if (info != null)
                        {
                            GlobalAR.currentFeature = info.feature;
                            GlobalAR.isViewDialog = true;
                        }

                        Debug.Log(hit[i].transform.name);
                    }
                }
            }
        }
        if (!GlobalAR.isWorldLoad) return;

        //if (Time.frameCount % 30 != 0 || !GlobalAR.dataManager.hasData) { return; }
        if (Time.frameCount % 30 != 0) { return; }

        if (firstRun)
        {
            firstRun = false;
            Info3DView(GlobalAR.dataManager.infoBox);
            Info3DView(GlobalAR.dataManager.warnBox);


        }
        update3DView(GlobalAR.outsideAR);
        if (GlobalAR.isViewDialog && !GlobalAR.isShowedDialog)
        {
            dialogInfo.SetActive(true);

        }

        if (GlobalAR.insideWarn.Count > 0 && GlobalAR.demoMode == GlobalAR.AnimationMode.None)
        {

            var feature = GlobalAR.insideWarn[0];

            Debug.Log(feature.properties.risk_type);
            if (feature.properties.risk_type == 0)
            {

                if (!isFire)
                {
                    isFire = true;
                    resetAR();
                    return;
                }

                GlobalAR.currentWarn = "fire";
                foreach (var plane in m_ARPlaneManager.trackables)
                {

                    var stringid = plane.trackableId.ToString();
                    if (planeAddFire.Contains(stringid))
                    {
                        continue;
                    }
                    //var distance = Mathf.Sqrt(Mathf.Pow(plane.transform.position.x - m_ARPlaneManager.transform.position.x, 2) +
                    //       Mathf.Pow(plane.transform.position.z - m_ARPlaneManager.transform.position.z, 2));
                    //if (distance > 15) continue;


                    GameObject obj = (GameObject)Resources.Load("Bonfire");
                    var rand = Random.Range(1.0f, 4.0f);
                    obj.transform.localScale = new Vector3(rand, rand, rand);
                    obj.transform.position = new Vector3(plane.transform.position.x, plane.transform.position.y, plane.transform.position.z);
                    Instantiate(obj);
                    if (plane.classification == PlaneClassification.Floor)
                    {
                        for (int i = 0; i <= 8; i++)
                        {
                            var randScale = Random.Range(1.0f, 4.0f);
                            obj.transform.localScale = new Vector3(randScale, randScale, randScale);
                            obj.transform.position = new Vector3(plane.transform.position.x + Random.Range(-7, 7), plane.transform.position.y, plane.transform.position.z + Random.Range(-7, 7));
                            Instantiate(obj);
                        }
                    }
                    planeAddFire.Add(stringid);
                }
            }
            else
            {
                planeAddFire = new List<string>();
                isFire = false;
            }

            if (feature.properties.risk_type == 1 || feature.properties.risk_type == 7)
            {
                occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
                meshManager.meshPrefab = occPrefab;

                if (!isWater)
                {
                    isWater = true;
                    resetAR();
                    return;
                }

                
                GlobalAR.currentWarn = "water";
                var water = WaterAction.transform.Find("water").gameObject;
                if (water != null)
                {
                    water.GetComponent<FloodController>().heigth = (float)feature.properties.water_level;
                }
                WaterAction.SetActive(true);
                if (water != null)
                {
                    water.GetComponent<FloodController>().heigth = (float)feature.properties.water_level;
                }

            }
            else
            {
                isWater = false;
                WaterAction.SetActive(false);
                occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                meshManager.meshPrefab = nonePrefab;
            }

            if (feature.properties.risk_type == 8)
            {
                addLiquefaction();

            }
            else
            {
                isLique = false;
                LiqueAction.SetActive(false);
                occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                meshManager.meshPrefab = nonePrefab;
            }

            if (feature.properties.risk_type == 2)
            {
                GlobalAR.currentWarn = "hill";
                hillAnimation();
            }
            else
            {
                isHill = false;

            }
            if (feature.properties.risk_type == 3 || feature.properties.risk_type == 4 || feature.properties.risk_type == 6)
            {
                GlobalAR.currentWarn = "house";
                hosueAnimation();
            }
            else
            {
                isHouse = false;
            }
            if (feature.properties.risk_type == 5)
            {
                if (!isWall)
                {
                    isWall = true;
                    resetAR();
                    return;
                }
                GlobalAR.currentWarn = "wall";
                foreach (var plane in m_ARPlaneManager.trackables)
                {
                    var stringid = plane.trackableId.ToString();
                    if (plane.classification == PlaneClassification.Wall)
                    {
                        if (planeAddBlock.Contains(stringid))
                        {
                            continue;
                        }

                        //var distance = Mathf.Sqrt(Mathf.Pow(plane.transform.position.x - m_ARPlaneManager.transform.position.x, 2) +
                        //    Mathf.Pow(plane.transform.position.z - m_ARPlaneManager.transform.position.z, 2));
                        //if (distance > 15) continue;
                        GameObject obj = (GameObject)Resources.Load("Wall");

                        obj.transform.position = new Vector3(plane.transform.position.x, plane.transform.position.y, plane.transform.position.z);
                        obj.transform.forward = plane.transform.forward;
                        obj.transform.Rotate(90, 180, 0);
                        Instantiate(obj);
                        planeAddBlock.Add(stringid);
                    }

                }
            }
            else
            {
                isWall = false;
                planeAddBlock = new List<string>();
            }
            if (GlobalAR.currentWarn == "house" )
            {
                if (!houseAudio.isPlaying)
                {
                    houseAudio.Play();
                }


            }
            else
            {
                houseAudio.Pause();
            }
            if ( GlobalAR.currentWarn == "hill")
            {
                if (!rockFallAudio.isPlaying)
                {
                    rockFallAudio.Play();
                }


            }
            else
            {
                rockFallAudio.Pause();
            }
        }
        else
        {
            
            if (GlobalAR.demoMode == GlobalAR.AnimationMode.None)
            {
                GlobalAR.currentWarn = "";
                rockFallAudio.Pause();
                houseAudio.Pause();
                WaterAction.SetActive(false);
                occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                meshManager.meshPrefab = nonePrefab;
            }
            else
            {
                if (GlobalAR.demoMode == GlobalAR.AnimationMode.Fire)
                {

                    if (!isFire)
                    {
                        isFire = true;
                        resetAR();
                        return;
                    }

                    GlobalAR.currentWarn = "fire";
                    foreach (var plane in m_ARPlaneManager.trackables)
                    {

                        var stringid = plane.trackableId.ToString();
                        if (planeAddFire.Contains(stringid))
                        {
                            continue;
                        }
                        //var distance = Mathf.Sqrt(Mathf.Pow(plane.transform.position.x - m_ARPlaneManager.transform.position.x, 2) +
                        //       Mathf.Pow(plane.transform.position.z - m_ARPlaneManager.transform.position.z, 2));
                        //if (distance > 15) continue;


                        GameObject obj = (GameObject)Resources.Load("Bonfire");
                        var rand = Random.Range(1.0f, 4.0f);
                        obj.transform.localScale = new Vector3(rand, rand, rand);
                        obj.transform.position = new Vector3(plane.transform.position.x, plane.transform.position.y, plane.transform.position.z);
                        Instantiate(obj);
                        if (plane.classification == PlaneClassification.Floor)
                        {
                            for (int i = 0; i <= 8; i++)
                            {
                                var randScale = Random.Range(1.0f, 4.0f);
                                obj.transform.localScale = new Vector3(randScale, randScale, randScale);
                                obj.transform.position = new Vector3(plane.transform.position.x + Random.Range(-7, 7), plane.transform.position.y , plane.transform.position.z + Random.Range(-7, 7));
                                Instantiate(obj);
                            }
                        }
                        planeAddFire.Add(stringid);
                    }
                }
                else
                {
                    planeAddFire = new List<string>();
                    isFire = false;
                }

                if (GlobalAR.demoMode == GlobalAR.AnimationMode.Water)
                {
                    occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
                    meshManager.meshPrefab = occPrefab;

                    if (!isWater)
                    {
                        isWater = true;
                        resetAR();
                        return;
                    }


                    GlobalAR.currentWarn = "water";
                    var water = WaterAction.transform.Find("water").gameObject;
                    if (water != null)
                    {
                        water.GetComponent<FloodController>().heigth = 1;
                    }
                    WaterAction.SetActive(true);
                    if (water != null)
                    {
                        water.GetComponent<FloodController>().heigth = 1;
                    }

                }
                else
                {
                    isWater = false;
                    WaterAction.SetActive(false);
                    occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                    meshManager.meshPrefab = nonePrefab;
                }

                if(GlobalAR.demoMode == GlobalAR.AnimationMode.Lique)
                {
                    addLiquefaction();
                }
                else
                {
                    isLique = false;
                    //LiqueAction.SetActive(false);
                    occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
                    meshManager.meshPrefab = nonePrefab;
                }

                if (GlobalAR.demoMode == GlobalAR.AnimationMode.Stone)
                {
                    GlobalAR.currentWarn = "hill";
                    hillAnimation();
                }
                else
                {
                    isHill = false;

                }
                if (GlobalAR.demoMode == GlobalAR.AnimationMode.Home)
                {
                    GlobalAR.currentWarn = "house";
                    hosueAnimation();
                }
                else
                {
                    isHouse = false;
                }
                if (GlobalAR.demoMode == GlobalAR.AnimationMode.Wall)
                {
                    if (!isWall)
                    {
                        isWall = true;
                        resetAR();
                        return;
                    }
                    GlobalAR.currentWarn = "wall";
                    foreach (var plane in m_ARPlaneManager.trackables)
                    {
                        var stringid = plane.trackableId.ToString();
                        if (plane.classification == PlaneClassification.Wall)
                        {
                            if (planeAddBlock.Contains(stringid))
                            {
                                continue;
                            }

                            //var distance = Mathf.Sqrt(Mathf.Pow(plane.transform.position.x - m_ARPlaneManager.transform.position.x, 2) +
                            //    Mathf.Pow(plane.transform.position.z - m_ARPlaneManager.transform.position.z, 2));
                            //if (distance > 15) continue;
                            GameObject obj = (GameObject)Resources.Load("Wall");

                            obj.transform.position = new Vector3(plane.transform.position.x, plane.transform.position.y, plane.transform.position.z);
                            obj.transform.forward = plane.transform.forward;

                            obj.transform.Rotate(90, 180, 0);
                            Instantiate(obj);
                            planeAddBlock.Add(stringid);
                        }

                    }
                }
                else
                {
                    isWall = false;
                    planeAddBlock = new List<string>();
                }
                if (GlobalAR.currentWarn == "house")
                {
                    if (!houseAudio.isPlaying)
                    {
                        houseAudio.Play();
                    }


                }
                else
                {
                    houseAudio.Pause();
                }

                if (GlobalAR.currentWarn == "hill")
                {
                    if (!rockFallAudio.isPlaying)
                    {
                        rockFallAudio.Play();
                    }


                }
                else
                {
                    rockFallAudio.Pause();
                }
            }
        }

    }

    void updateNotice()
    {
        if (GlobalAR.nearWarn.Count > 0 || GlobalAR.insideWarn.Count > 0)
        {
            if (noticeMsg.Count == 0)
            {
                foreach (JSONModel.Feature f in GlobalAR.insideWarn)
                {
                    noticeMsg.Add(f.properties.message2);
                }
                foreach (JSONModel.Feature f in GlobalAR.nearWarn)
                {
                    noticeMsg.Add(f.properties.message1);
                }
            }
            notice.SetActive(true);
        }
        else
        {
            notice.SetActive(false);
        }
        if (noticeMsg.Count > 0)
        {
            var text = GameObject.Find("status").GetComponent<Text>();
            var textMsg = noticeMsg[0];
            text.text = textMsg;
            noticeMsg.Remove(textMsg);
            notice.SetActive(true);
        }

    }

    IEnumerator GetText()
    {
        var url = GetDisasterLink();
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
            var text = www.downloadHandler.text;
            var myObject = JsonConvert.DeserializeObject<Root>(text);

            GlobalAR.dataManager.storeData(myObject);
            //Debug.LogError(myObject.type);
        }
    }

    public string GetDisasterLink()
    {
        string jsonFilePath = Path.Combine(Application.persistentDataPath, "disaster.json");

        if (File.Exists(jsonFilePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);

                // JSON データを JSONData クラスにデシリアライズする
                Disaster jsonData = JsonUtility.FromJson<Disaster>(jsonContent);
                string disasterLink = jsonData.disaster_link;
                Debug.Log(disasterLink);
                return disasterLink;
            }
            catch (System.Exception e)
            {
                Debug.LogError("JSON ファイルの読み込み中にエラーが発生しました: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("JSON ファイルが見つかりません。");
        }

        return ""; // エラーが発生した場合、またはファイルが存在しない場合は "" を返す
    }
    void Info3DView(List<JSONModel.Feature> arrF)
    {
        foreach (JSONModel.Feature f in arrF)
        {


            GeoCoordinate target = new GeoCoordinate(f.geometry.coordinates[1], f.geometry.coordinates[0]);
            var distance = target.GetDistanceTo(GlobalAR.currentlocation);
            if (f.properties.info_type == GlobalAR.kWarn)
            {
                if (f.properties.range != null)
                {
                    var range = (float)f.properties.range;
                    distance = distance - range;
                }
            }
            if (distance > GlobalAR.disCam)
            {
                GlobalAR.outsideAR.Add(f);
            }
            else
            {
                string objName = "icon_infoTag";
                if (f.properties.info_type == GlobalAR.kWarn)
                {
                    objName = "icon_warn3";
                }
                if (!string.IsNullOrEmpty(f.properties.icon))
                    objName = f.properties.icon.Replace(".png", "").Replace(".jpg", "");

                GameObject obj = (GameObject)Resources.Load(objName);
                if (obj == null)
                {
                    if (f.properties.info_type == GlobalAR.kWarn)
                    {
                        obj = (GameObject)Resources.Load("icon_warn3");
                    }
                    else
                    {
                        continue;
                    }

                }
                GlobalAR.insideAR.Add(f);
                //GameObject instance = Instantiate(obj, GeoTool.ConvertCoordinateFromCurrentLocation(target, GlobalAR.heightAR, 0f), Quaternion.identity) as GameObject;
                GameObject instance = Instantiate(obj, GeoTool.ConvertGPS2Position(target, GlobalAR.heightAR, m_CameraManager.transform.position), Quaternion.identity) as GameObject;
                InfoModelController info = instance.GetComponent("InfoModelController") as InfoModelController;
                info.feature = f;
            }
        }
    }
    void update3DView(List<JSONModel.Feature> arrF)
    {
        foreach (JSONModel.Feature f in arrF)
        {
            string objName = "icon_infoTag";
            if (f.properties.info_type == GlobalAR.kWarn)
            {
                objName = "icon_warn3";
            }
            if (!string.IsNullOrEmpty(f.properties.icon))
                objName = f.properties.icon.Replace(".png", "").Replace(".jpg", "");
            GameObject obj = (GameObject)Resources.Load(objName);
            if (obj == null)
            {
                if (f.properties.info_type == GlobalAR.kWarn)
                {
                    obj = (GameObject)Resources.Load("icon_warn3");
                }
                else
                {
                    continue;
                }

            }

            GeoCoordinate target = new GeoCoordinate(f.geometry.coordinates[1], f.geometry.coordinates[0]);
            var distance = target.GetDistanceTo(GlobalAR.currentlocation);
            if (f.properties.info_type == GlobalAR.kWarn)
            {
                if (f.properties.range != null)
                {
                    var range = (float)f.properties.range;
                    distance = distance - range;
                }
            }
            if (distance > GlobalAR.disCam)
            {
                //GlobalAR.outsideAR.Add(f);
            }
            else
            {
                if (!GlobalAR.insideAR.Contains(f))
                {
                    GlobalAR.insideAR.Add(f);
                    //GameObject instance = Instantiate(obj, GeoTool.ConvertCoordinateFromCurrentLocation(target, GlobalAR.heightAR, 0f), Quaternion.identity) as GameObject;
                    GameObject instance = Instantiate(obj, GeoTool.ConvertGPS2Position(target, GlobalAR.heightAR, m_CameraManager.transform.position), Quaternion.identity) as GameObject;
                    InfoModelController info = instance.GetComponent("InfoModelController") as InfoModelController;
                    info.feature = f;
                }

            }


        }
    }

    void hillAnimation()
    {

        addGameObjectRandom("rock1");
        addGameObjectRandom("rock2");
        addGameObjectRandom("rock3");
        addGameObjectRandom("rock4");
        addGameObjectRandom("rock5");
        addGameObjectRandom("rock6");

        addGameObjectRandom("rock1");
        addGameObjectRandom("rock2");
        addGameObjectRandom("rock3");
        addGameObjectRandom("rock4");
        addGameObjectRandom("rock5");
        addGameObjectRandom("rock6");


    }
    void hosueAnimation()
    {
        addGameObjectRandom("house part 1");
        addGameObjectRandom("house part 2");
        addGameObjectRandom("house part 3");
        addGameObjectRandom("house part 4");
        addGameObjectRandom("house part 5");

        addGameObjectRandom("house part 1");
        addGameObjectRandom("house part 2");
        addGameObjectRandom("house part 3");
        addGameObjectRandom("house part 4");
        addGameObjectRandom("house part 5");

    }

    void addGameObjectRandom(string objectName)
    {
        var limit = Random.Range(5, 10);
        for (int i = 0; i < limit; i++)
        {
            GameObject obj = (GameObject)Resources.Load(objectName);
            var pos = m_CameraManager.transform.position;
            obj.transform.localPosition = new Vector3(pos.x + Random.Range(-4.0f, 4.0f), pos.y + 5, pos.z + Random.Range(-4.0f, 4.0f));
            obj.transform.rotation = Random.rotation;
            Instantiate(obj);
        }
    }
    public Vector3 ConvertGPS2ARCoordinate(JSONModel.Feature ft)
    {
        double dz = (ft.geometry.coordinates[1] - GlobalAR.userLat) * GlobalAR.lat2km;   // -z????????
        double dx = -(ft.geometry.coordinates[0] - GlobalAR.userLng) * GlobalAR.lat2km; // +x????????
        return new Vector3((float)dx, 0, (float)dz);
    }
    public void printXYZ(Vector3 vc)
    {
        Debug.Log(vc.x + " ; " + vc.y + ";" + vc.z);
    }
    public void printXYZW(Quaternion vc)
    {
        Debug.Log(vc.x + " ; " + vc.y + ";" + vc.z + ";" + vc.w);
    }
    public void addLiquefaction()
    {
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
        meshManager.meshPrefab = occPrefab;

        if (!isLique)
        {
            isLique = true;
            resetAR();
            return;
        }


        GlobalAR.currentWarn = "lique";
        //LiqueAction.SetActive(true);
        foreach (var plane in m_ARPlaneManager.trackables)
        {

            var stringid = plane.trackableId.ToString();
            if (planeLiqueBlock.Contains(stringid))
            {
                continue;
            }
            //var distance = Mathf.Sqrt(Mathf.Pow(plane.transform.position.x - m_ARPlaneManager.transform.position.x, 2) +
            //       Mathf.Pow(plane.transform.position.z - m_ARPlaneManager.transform.position.z, 2));
            //if (distance > 15) continue;

            GameObject TreeSwing = (GameObject)Resources.Load("TreeSwing");
            //GameObject LancanSwing = (GameObject)Resources.Load("LancanSwing");
            GameObject Manhole = (GameObject)Resources.Load("manhole");

            if (plane.classification == PlaneClassification.Floor)
            {
                TreeSwing.transform.position = new Vector3(plane.transform.position.x, plane.transform.position.y, plane.transform.position.z);
                Instantiate(TreeSwing);
                //LancanSwing.transform.position = new Vector3(plane.transform.position.x - 3, plane.transform.position.y, plane.transform.position.z);
                //Instantiate(LancanSwing);
                Manhole.transform.position = new Vector3(plane.transform.position.x + 1, plane.transform.position.y, plane.transform.position.z);
                Instantiate(Manhole);
                ARPlane ar = new ARPlane();
                for (int i = 0; i < 1; i++)
                {
                    int randomValue = Random.Range(0, 1) == 0 ? -1 : 1;
                    float x = plane.transform.position.x + Random.Range(4, 7)* randomValue;
                    
                    float z = plane.transform.position.z + Random.Range(4, 7)* randomValue;
                    TreeSwing.transform.position = new Vector3(x, plane.transform.position.y, z);
                    Instantiate(TreeSwing);
                    Manhole.transform.position = new Vector3(x + 1, plane.transform.position.y, z);
                    Instantiate(Manhole);
                }

            }

            planeLiqueBlock.Add(stringid);
        }
    }
    public void resetAR()
    {
        var tag1_Objects = GameObject.FindGameObjectsWithTag("ARViewer");

        for (int i = 0; i < tag1_Objects.Length; i++)
        {
            Destroy(tag1_Objects[i]);
        }
        m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();
        m_CameraManager = FindObjectOfType<ARCameraManager>();
        m_ARSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        m_ARSession = FindObjectOfType<ARSession>();
        noticeMsg = new List<string>();
        planeAddFire = new List<string>();
        planeAddBlock = new List<string>();
        planeLiqueBlock = new List<string>();
        GlobalAR.insideWarn = new List<JSONModel.Feature>();
        GlobalAR.nearWarn = new List<JSONModel.Feature>();
        GlobalAR.insideAR = new List<JSONModel.Feature>();
        GlobalAR.outsideAR = new List<JSONModel.Feature>();
        GlobalAR.isWorldLoad = false;
        firstRun = true;
        dialogInfo.SetActive(false);
        notice.SetActive(false);
#if UNITY_IOS
        var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
#else
            XRSessionSubsystem sessionSubsystem = null;
#endif
        if (sessionSubsystem == null)
            return;

#if UNITY_IOS
        m_ARSession.Reset();
#endif
       
    }
}
