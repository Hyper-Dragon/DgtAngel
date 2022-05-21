using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainLightSwitch : MonoBehaviour
{
    Light mainLight;

    // Start is called before the first frame update
    void Start()
    {
        mainLight = GameObject.Find("MainLight").GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            mainLight.enabled = !mainLight.enabled;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && mainLight.intensity <= 2.5f)
        {
            mainLight.intensity += .1f;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) && mainLight.intensity > .3f) 
        {
            mainLight.intensity -= .1f;
        }


    }
}