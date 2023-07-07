using System.Collections;
using UnityEngine;
using System.Device.Location;

/// <summary>�o�ܓx�擾�N���X</summary>
public class LocationData : MonoBehaviour
{
    /// <summary>�o�ܓx�擾�Ԋu�i�b�j</summary>
    private const float IntervalSeconds = 1.0f;

    /// <summary>���P�[�V�����T�[�r�X�̃X�e�[�^�X</summary>
    private LocationServiceStatus locationServiceStatus;

    /// <summary>�o�x</summary>
    public double Longitude { get; private set; }

    /// <summary>�ܓx</summary>
    public double Latitude { get; private set; }

    /// <summary>�ܓx�o�x��񂪎擾�\��</summary>
    /// <returns>�\�Ȃ�true�A�s�\�Ȃ�false</returns>
    public bool CanGetLonLat()
    {
        return Input.location.isEnabledByUser;
    }

    /// <summary>�o�ܓx�擾����</summary>
    /// <returns>�����Ԗ��ɔ񓯊����s���邽�߂̖߂�l</returns>
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