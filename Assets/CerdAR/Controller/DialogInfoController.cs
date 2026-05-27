using JSONModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Device.Location;
using System.Runtime.InteropServices;

public class DialogInfoController : MonoBehaviour
{
    public Text title;
    public Text distance;
    public Text content;
    public RawImage image;
    public GameObject main;

    // Start is called before the first frame update
    void Start()
    {
        

    }

    private void Awake()
    {
        Debug.Log(isActiveAndEnabled);
    }
    private void OnEnable()
    {
        Debug.Log("OnEnable");
    }
    private void OnDisable()
    {
        Debug.Log("OnDisable");
    }
    // Update is called once per frame
    void Update()
    {
        if (GlobalAR.isShowedDialog)
        {
            setFeature(GlobalAR.currentFeature);
            main.SetActive(true);
        }
    }

    public void setFeature(Feature feature)
    {
        var name = feature.properties.name;
        if (name == null || name == "Unnamed Layer")
        {
            title.text = feature.properties.Name;
        }
        else
        {
            title.text = name;
        }
        
        GeoCoordinate target = new GeoCoordinate(feature.geometry.coordinates[1], feature.geometry.coordinates[0]);
        double dis = GlobalAR.currentlocation.GetDistanceTo(target);
        if (feature.properties.info_type == GlobalAR.kInfo) 
        { 
            if(feature.properties.pic_type  == GlobalAR.kPhoto)
            {
                if(feature.properties.photo != null)
                {
                    URI = feature.properties.photo;
                    StartCoroutine(StartDowload());
                }
            }
            else if(feature.properties.pic_type == GlobalAR.kMovie)
            {
                var texture = Resources.Load<Texture2D>("png/youtube");
                image.texture = texture;
            }
            else
            {
                var texture = Resources.Load<Texture2D>("png/"+feature.properties.icon.Replace(".png","").Replace(".jpg",""));
                if (texture != null)
                image.texture = texture;
            }
        }
        else
        {
            var texture = Resources.Load<Texture2D>("png/" + feature.properties.icon.Replace(".png", "").Replace(".jpg", ""));
            if (texture == null)
                texture = Resources.Load<Texture2D>("png/icon_warn3");
            
            image.texture = texture;
            if (feature.properties.range != null)
            {
                dis = dis - (double)feature.properties.range;
                if (dis <= 0)
                {
                    dis = 0;
                }

            }
        }
        

        distance.text ="距離： "+ dis.ToString("F0") + "m";
        content.text = feature.properties.description;
        GlobalAR.isShowedDialog = false;
    }
    public void close()
    {
        GlobalAR.isShowedDialog = false;
        GlobalAR.isViewDialog = false;
        main.SetActive(false);
    }
    #if UNITY_IPHONE
[DllImport("__Internal")]
private static extern void _LaunchSafariView(string url);
#endif

public void openYoutube()
{
    if (GlobalAR.currentFeature.properties.pic_type == GlobalAR.kMovie &&
        GlobalAR.currentFeature.properties.movie != null)
    {
        string rawUrl = GlobalAR.currentFeature.properties.movie;
        string finalUrl = rawUrl;

        string videoId = "";
        if (rawUrl.Contains("youtu.be/"))
        {
            videoId = rawUrl.Split(new string[] { "youtu.be/" }, System.StringSplitOptions.None)[1].Split('?')[0];
        }
        else if (rawUrl.Contains("v="))
        {
            var query = rawUrl.Split(new string[] { "v=" }, System.StringSplitOptions.None)[1];
            videoId = query.Split('&')[0];
        }
        else if (rawUrl.Contains("/embed/"))
        {
            videoId = rawUrl.Split(new string[] { "/embed/" }, System.StringSplitOptions.None)[1].Split('?')[0];
        }

        if (!string.IsNullOrEmpty(videoId))
        {
            finalUrl = "https://www.youtube.com/watch?v=" + videoId;
        }

        close();

        Debug.Log("Launching Safari View Controller: " + finalUrl);

#if UNITY_IPHONE && !UNITY_EDITOR
        SafariCallbackReceiver.MuteAndPrepare();
        _LaunchSafariView(finalUrl);
#else
        Application.OpenURL(finalUrl);
#endif
    }
}
    public string URI = "https://www.musubou.net/musubou-ar/AR_logo.jpg/AR_logo.jpg";

    IEnumerator StartDowload()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(URI);

        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            image.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        }
    }
}

public class SafariCallbackReceiver : MonoBehaviour
{
    private static float previousVolume = 1f;

    public static void MuteAndPrepare()
    {
        previousVolume = AudioListener.volume;
        AudioListener.volume = 0f;

        if (GameObject.Find("SafariCallbackReceiver") == null)
        {
            GameObject go = new GameObject("SafariCallbackReceiver");
            go.AddComponent<SafariCallbackReceiver>();
            DontDestroyOnLoad(go);
        }
    }

    public void OnSafariClosed(string message)
    {
        Debug.Log("Safari View Controller closed. Restoring audio.");
        AudioListener.volume = previousVolume;
    }
}