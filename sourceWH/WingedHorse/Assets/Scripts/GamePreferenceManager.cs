using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePreferenceManager : MonoBehaviour
{
    private const string IS_CLOCK_ON = "IS_CLOCK_ON";

    // Start is called before the first frame update
    void Start()
    {
        LoadPrefs();
    }

    //void OnApplicationQuit()
    //{
    //    SavePrefs();
    //}

    public static void SavePrefs()
    {
        PlayerPrefs.SetInt(IS_CLOCK_ON, BoardControl.isClockDisplayOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadPrefs()
    {
        BoardControl.isClockDisplayOn = PlayerPrefs.GetInt(IS_CLOCK_ON)==1?true:false;
    }

}
