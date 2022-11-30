using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePreferenceManager : MonoBehaviour
{
    private static GameObject board;
    private static GameObject mainLight;
    private static GameObject mainCamera;

    private GameObject squareTextA1;
    private GameObject squareTextH8;

    private const string IS_CLOCK_ON = "IS_CLOCK_ON";

    //private const string CAM_POS_X = "CAM_POS_X";
    //private const string CAM_POS_Y = "CAM_POS_Y";
    //private const string CAM_POS_Z = "CAM_POS_Z";
    //private const string CAM_ROT_X = "CAM_ROT_X";
    //private const string CAM_ROT_Y = "CAM_ROT_Y";
    //private const string CAM_ROT_Z = "CAM_ROT_Z";
    //private const string CAM_ROT_W = "CAM_ROT_W";

    private const string LIGHT_POS_X = "LIGHT_POS_X";
    private const string LIGHT_POS_Y = "LIGHT_POS_Y";
    private const string LIGHT_POS_Z = "LIGHT_POS_Z";
    private const string LIGHT_ROT_X = "LIGHT_ROT_X";
    private const string LIGHT_ROT_Y = "LIGHT_ROT_Y";
    private const string LIGHT_ROT_Z = "LIGHT_ROT_Z";
    private const string LIGHT_ROT_W = "LIGHT_ROT_W";

    private const string BOARD_POS_X = "BOARD_POS_X";
    private const string BOARD_POS_Y = "BOARD_POS_Y";
    private const string BOARD_POS_Z = "BOARD_POS_Z";

    // Start is called before the first frame update
    void Start()
    {
        //mainCamera = GameObject.Find("MainCamera");
        mainLight = GameObject.Find("MainLight");
        board = GameObject.Find("Board");

        squareTextA1 = GameObject.Find("Board/Ranks/Rank1/SquareText");
        squareTextH8 = GameObject.Find("Board/Ranks/Rank8/SquareText");
        
        LoadPrefs();
    }

    //void OnApplicationQuit()
    //{
    //    SavePrefs();
    //}

    public static void SavePrefs()
    {
        PlayerPrefs.SetInt(IS_CLOCK_ON, BoardControl.isClockDisplayOn ? 1 : 0);

        //PlayerPrefs.SetFloat(CAM_POS_X, mainCamera.transform.position.x);
        //PlayerPrefs.SetFloat(CAM_POS_Y, mainCamera.transform.position.y);
        //PlayerPrefs.SetFloat(CAM_POS_Z, mainCamera.transform.position.z);
        //PlayerPrefs.SetFloat(CAM_ROT_X, mainCamera.transform.rotation.x);
        //PlayerPrefs.SetFloat(CAM_ROT_Y, mainCamera.transform.rotation.y);
        //PlayerPrefs.SetFloat(CAM_ROT_Z, mainCamera.transform.rotation.z);
        //PlayerPrefs.SetFloat(CAM_ROT_W, mainCamera.transform.rotation.w);

        PlayerPrefs.SetFloat(LIGHT_POS_X, mainLight.transform.position.x);
        PlayerPrefs.SetFloat(LIGHT_POS_Y, mainLight.transform.position.y);
        PlayerPrefs.SetFloat(LIGHT_POS_Z, mainLight.transform.position.z);
        PlayerPrefs.SetFloat(LIGHT_ROT_X, mainLight.transform.rotation.x);
        PlayerPrefs.SetFloat(LIGHT_ROT_Y, mainLight.transform.rotation.y);
        PlayerPrefs.SetFloat(LIGHT_ROT_Z, mainLight.transform.rotation.z);
        PlayerPrefs.SetFloat(LIGHT_ROT_W, mainLight.transform.rotation.w);

        PlayerPrefs.SetFloat(BOARD_POS_X, board.transform.position.x);
        PlayerPrefs.SetFloat(BOARD_POS_Y, board.transform.position.y);
        PlayerPrefs.SetFloat(BOARD_POS_Z, board.transform.position.z);

        PlayerPrefs.Save();
    }

    public void LoadPrefs()
    {
        BoardControl.isClockDisplayOn = PlayerPrefs.GetInt(IS_CLOCK_ON)==1;

        //float camPosX = PlayerPrefs.GetFloat(CAM_POS_X);
        //float camPosY = PlayerPrefs.GetFloat(CAM_POS_Y);
        //float camPosZ = PlayerPrefs.GetFloat(CAM_POS_Z);
        //float camRotX = PlayerPrefs.GetFloat(CAM_ROT_X);
        //float camRotY = PlayerPrefs.GetFloat(CAM_ROT_Y);
        //float camRotZ = PlayerPrefs.GetFloat(CAM_ROT_Z);
        //float camRotW = PlayerPrefs.GetFloat(CAM_ROT_W);

        float lightPosX = PlayerPrefs.GetFloat(LIGHT_POS_X);
        float lightPosY = PlayerPrefs.GetFloat(LIGHT_POS_Y);
        float lightPosZ = PlayerPrefs.GetFloat(LIGHT_POS_Z);
        float lightRotX = PlayerPrefs.GetFloat(LIGHT_ROT_X);
        float lightRotY = PlayerPrefs.GetFloat(LIGHT_ROT_Y);
        float lightRotZ = PlayerPrefs.GetFloat(LIGHT_ROT_Z);
        float lightRotW = PlayerPrefs.GetFloat(LIGHT_ROT_W);

        float boardPosX = PlayerPrefs.GetFloat(BOARD_POS_X);
        float boardPosY = PlayerPrefs.GetFloat(BOARD_POS_Y);
        float boardPosZ = PlayerPrefs.GetFloat(BOARD_POS_Z);

        //mainCamera.transform.position = new Vector3(camPosX, camPosY, camPosZ);
        //mainCamera.transform.rotation = new Quaternion(camRotX, camRotY, camRotZ, camRotW);

        mainLight.transform.position = new Vector3(lightPosX, lightPosY, lightPosZ);
        mainLight.transform.rotation = new Quaternion(lightRotX, lightRotY, lightRotZ, lightRotW);

        board.transform.position = new Vector3(boardPosX, boardPosY, boardPosZ);
    }
}
