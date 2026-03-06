using JSONModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Device.Location;
using System.Runtime.InteropServices; // これを忘れずに

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
        if (!GlobalAR.isShowedDialog)
        {
            main.SetActive(true);
            setFeature(GlobalAR.currentFeature);
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
            if (feature.properties.pic_type == GlobalAR.kPhoto)
            {
                if (feature.properties.photo != null)
                {
                    URI = feature.properties.photo;
                    StartCoroutine(StartDowload());
                }
            }
            else if (feature.properties.pic_type == GlobalAR.kMovie)
            {
                var texture = Resources.Load<Texture2D>("png/youtube");
                image.texture = texture;
            }
            else
            {
                var texture = Resources.Load<Texture2D>("png/" + feature.properties.icon.Replace(".png", "").Replace(".jpg", ""));
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


        distance.text = "距離： " + dis.ToString("F0") + "m";
        content.text = feature.properties.description;
        GlobalAR.isShowedDialog = false;
    }
    public void close()
    {
        GlobalAR.isShowedDialog = false;
        GlobalAR.isViewDialog = false;
        main.SetActive(false);
    }


    // iOSプラグインの定義
#if UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern void _LaunchSafariView(string url);
#endif

public void openYoutube()
    {
        if (GlobalAR.currentFeature.properties.pic_type == GlobalAR.kMovie && GlobalAR.currentFeature.properties.movie != null)
        {
            string rawUrl = GlobalAR.currentFeature.properties.movie;
            string finalUrl = rawUrl;

            // 1. URLから動画IDを抽出する
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

            // 2. IDが取れたら「普通の視聴ページ」のURLを作る
            if (!string.IsNullOrEmpty(videoId))
            {
                finalUrl = "https://www.youtube.com/watch?v=" + videoId;
            }

            // ▼▼▼ 追加：ブラウザを開く前に、自分のウィンドウを閉じる ▼▼▼
            close();
            // ▲▲▲ 追加ここまで ▲▲▲

            Debug.Log("Launching Safari View Controller: " + finalUrl);

            #if UNITY_IPHONE && !UNITY_EDITOR
            // iOS実機の場合: 音声をミュートし、コールバック受信準備をしてからSafariを開く
            SafariCallbackReceiver.MuteAndPrepare();
            _LaunchSafariView(finalUrl);
            #else
            // エディタやAndroidの場合: 標準ブラウザに飛ぶため、アプリ自体がバックグラウンドに回り自動で無音になります
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
} // ← クラスの終わりはここ！

public class SafariCallbackReceiver : MonoBehaviour
{
    private static float previousVolume = 1f;

    // ミュートを実行し、通知を受け取るオブジェクトをシーンに準備する
    public static void MuteAndPrepare()
    {
        // 現在の音量を記憶してミュート
        previousVolume = AudioListener.volume;
        AudioListener.volume = 0f; 

        // 受信用の隠しゲームオブジェクトを生成（既に存在していれば作らない）
        if (GameObject.Find("SafariCallbackReceiver") == null)
        {
            GameObject go = new GameObject("SafariCallbackReceiver");
            go.AddComponent<SafariCallbackReceiver>();
            DontDestroyOnLoad(go); // シーンが切り替わっても破棄されないようにする
        }
    }

    // iOS側(SafariLauncher.mm)からSafariが閉じられた時に呼ばれるメソッド
    public void OnSafariClosed(string message)
    {
        Debug.Log("Safari View Controller closed. Restoring audio.");
        // 音量をミュート前の状態に戻す
        AudioListener.volume = previousVolume;
    }
}