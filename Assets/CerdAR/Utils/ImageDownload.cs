using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageDownload : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private RawImage _image;
    public  string URI = "https://4.bp.blogspot.com/-4xxTe_qeV1E/Vd7FkNUlwjI/AAAAAAAAxFc/8u9MNKtg7gg/s800/syachiku.png";
    IEnumerator Start()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(URI);

        //�摜���擾�ł���܂ő҂�
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            //�擾�����摜�̃e�N�X�`����RawImage�̃e�N�X�`���ɒ���t����
            _image.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
