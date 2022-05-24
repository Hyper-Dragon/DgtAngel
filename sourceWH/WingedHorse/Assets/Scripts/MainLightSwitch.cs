using UnityEngine;

public class MainLightSwitch : MonoBehaviour
{
    private Light mainLight;

    // Start is called before the first frame update
    void Start()
    {
        mainLight = GameObject.Find("MainLight").GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        // Light on/off switch and dimmer control (if the light is off turn on if the dimmer controls are used)
        mainLight.enabled = Input.GetKeyDown(KeyCode.L) 
                            ? !mainLight.enabled : Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || mainLight.enabled;
        mainLight.intensity -= (Input.GetKeyDown(KeyCode.Alpha1) && mainLight.intensity > .3f) ? .1f : 0f;
        mainLight.intensity += (Input.GetKeyDown(KeyCode.Alpha2) && mainLight.intensity < 2.5f) ? .1f : 0f;
    }
}