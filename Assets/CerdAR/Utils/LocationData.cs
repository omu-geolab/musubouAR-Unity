using System.Collections;
using UnityEngine;
using System.Device.Location;

/// <summary>経緯度取得クラス</summary>
public class LocationData : MonoBehaviour
{
    /// <summary>経緯度取得間隔（秒）</summary>
    private const float IntervalSeconds = 1.0f;

    /// <summary>ロケーションサービスのステータス</summary>
    private LocationServiceStatus locationServiceStatus;

    /// <summary>経度</summary>
    public double Longitude { get; private set; }

    /// <summary>緯度</summary>
    public double Latitude { get; private set; }

    /// <summary>緯度経度情報が取得可能か</summary>
    /// <returns>可能ならtrue、不可能ならfalse</returns>
    public bool CanGetLonLat()
    {
        return Input.location.isEnabledByUser;
    }

    /// <summary>経緯度取得処理</summary>
    /// <returns>一定期間毎に非同期実行するための戻り値</returns>
    void  Start()
    {
        InvokeRepeating("updateGPS",0.01f, 0.5f);
    }

    void updateGPS()
    {
        locationServiceStatus = Input.location.status;
        if (Input.location.isEnabledByUser)
        {
            switch (locationServiceStatus)
            {
                case LocationServiceStatus.Stopped:
                    Input.compass.enabled = true;
                    Input.location.Start(1,1);
                    break;
                case LocationServiceStatus.Running:
                    GlobalAR.heading = Input.compass.trueHeading;
                    GlobalAR.userLat = Input.location.lastData.latitude;
                    GlobalAR.userLng = Input.location.lastData.longitude;
                    GlobalAR.currentlocation = new GeoCoordinate(Input.location.lastData.latitude, Input.location.lastData.longitude);
                    break;
                default:
                    break;
            }
        }
    }

}