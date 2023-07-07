using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cardbroadMove : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    float buoyancy_force;
    Rigidbody rig;
    void Start()
    {
        rig=GetComponent<Rigidbody>();
        Vector3 force = new Vector3 (Random.Range(-0.5f, 0.5f), 0.0f, Random.Range(-0.5f, 0.5f));  
    //    // 力を設定
        rig.velocity  =  force;
    }

    // Update is called once per frame
    void Update()
    {
        // float y_pos= transform.position.y;
        // if(y_pos<0)
        // {S
        //     rig.AddForce(transform.up*buoyancy_force);  
        // } 
        // Vector3 force = new Vector3 (0.5f,0.0f,0.0f);  
        // rig.AddForce (force);  // 力を加える
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    StartCoroutine(Delay());
    //}
    //private IEnumerator Delay()
    //{
    //    rig.velocity = Vector3.zero;
    //    yield return new WaitForSeconds(5);
    //    rig.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 0.0f, Random.Range(-0.3f, 0.3f));
    //}
}
