using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePreferenceManager : MonoBehaviour
{
    private GameObject board;
    private GameObject mainLight;
    private Light mainLightComp;
    private Camera mainCamera;
    private GameObject squareTextA1;
    private GameObject squareTextH8;

    //Increment this is changing preferences
    private const int PREF_VERSION_NO = 5;

    private const string PREF_VERSION = "PREF_VERSION";
    private const string IS_CLOCK_ON = "IS_CLOCK_ON";
    private const string IS_LIGHT_ON = "IS_LIGHT_ON";
    private const string LIGHT_INTENSITY = "LIGHT_INTENSITY";
    private const string CAM_FOV = "CAM_FOV";

    private const string CAM_POS_X = "CAM_POS_X";
    private const string CAM_POS_Y = "CAM_POS_Y";
    private const string CAM_POS_Z = "CAM_POS_Z";
    private const string CAM_ROT_X = "CAM_ROT_X";
    private const string CAM_ROT_Y = "CAM_ROT_Y";
    private const string CAM_ROT_Z = "CAM_ROT_Z";
    private const string CAM_ROT_W = "CAM_ROT_W";

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
    private const string BOARD_ROT_X = "BOARD_ROT_X";
    private const string BOARD_ROT_Y = "BOARD_ROT_Y";
    private const string BOARD_ROT_Z = "BOARD_ROT_Z";
    private const string BOARD_ROT_W = "BOARD_ROT_W";

    private const string SQA1_ROT_X = "SQA1_ROT_X";
    private const string SQA1_ROT_Y = "SQA1_ROT_Y";
    private const string SQA1_ROT_Z = "SQA1_ROT_Z";
    private const string SQA1_ROT_W = "SQA1_ROT_W";

    private const string SQH8_ROT_X = "SQH8_ROT_X";
    private const string SQH8_ROT_Y = "SQH8_ROT_Y";
    private const string SQH8_ROT_Z = "SQH8_ROT_Z";
    private const string SQH8_ROT_W = "SQH8_ROT_W";

    // Start is called before the first frame update
    void Start()
    {
        board = GameObject.Find("Board");
        mainCamera = Camera.main;
        mainLight = GameObject.Find("MainLight");
        mainLightComp = GameObject.Find("MainLight").GetComponent<Light>();
        squareTextA1 = GameObject.Find("Board/Ranks/Rank1/SquareText");
        squareTextH8 = GameObject.Find("Board/Ranks/Rank8/SquareText");
            
        LoadPrefs();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            SavePrefs();
        }
    }

    public void SavePrefs()
    {
        PlayerPrefs.SetInt(PREF_VERSION, PREF_VERSION_NO);

        PlayerPrefs.SetInt(IS_CLOCK_ON, BoardControl.isClockDisplayOn ? 1 : 0);
        PlayerPrefs.SetInt(IS_LIGHT_ON, mainLightComp.enabled ? 1 : 0);
        PlayerPrefs.SetFloat(LIGHT_INTENSITY, mainLightComp.intensity);
        PlayerPrefs.SetFloat(CAM_FOV, mainCamera.fieldOfView);
        
        PlayerPrefs.SetFloat(CAM_POS_X, mainCamera.transform.position.x);
        PlayerPrefs.SetFloat(CAM_POS_Y, mainCamera.transform.position.y);
        PlayerPrefs.SetFloat(CAM_POS_Z, mainCamera.transform.position.z);
        PlayerPrefs.SetFloat(CAM_ROT_X, mainCamera.transform.rotation.x);
        PlayerPrefs.SetFloat(CAM_ROT_Y, mainCamera.transform.rotation.y);
        PlayerPrefs.SetFloat(CAM_ROT_Z, mainCamera.transform.rotation.z);
        PlayerPrefs.SetFloat(CAM_ROT_W, mainCamera.transform.rotation.w);

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
        PlayerPrefs.SetFloat(BOARD_ROT_X, board.transform.rotation.x);
        PlayerPrefs.SetFloat(BOARD_ROT_Y, board.transform.rotation.y);
        PlayerPrefs.SetFloat(BOARD_ROT_Z, board.transform.rotation.z);
        PlayerPrefs.SetFloat(BOARD_ROT_W, board.transform.rotation.w);

        PlayerPrefs.SetFloat(SQA1_ROT_X, squareTextA1.transform.rotation.x);
        PlayerPrefs.SetFloat(SQA1_ROT_Y, squareTextA1.transform.rotation.y);
        PlayerPrefs.SetFloat(SQA1_ROT_Z, squareTextA1.transform.rotation.z);
        PlayerPrefs.SetFloat(SQA1_ROT_W, squareTextA1.transform.rotation.w);

        PlayerPrefs.SetFloat(SQH8_ROT_X, squareTextH8.transform.rotation.x);
        PlayerPrefs.SetFloat(SQH8_ROT_Y, squareTextH8.transform.rotation.y);
        PlayerPrefs.SetFloat(SQH8_ROT_Z, squareTextH8.transform.rotation.z);
        PlayerPrefs.SetFloat(SQH8_ROT_W, squareTextH8.transform.rotation.w);

        PlayerPrefs.Save();
    }

    public void LoadPrefs()
    {
        // Only load if prefs saved and pref version number matches
        if (PlayerPrefs.HasKey(PREF_VERSION) &&
           PlayerPrefs.GetInt(PREF_VERSION) == PREF_VERSION_NO)
        {
            BoardControl.isClockDisplayOn = PlayerPrefs.GetInt(IS_CLOCK_ON) == 1;
            mainLightComp.enabled = PlayerPrefs.GetInt(IS_LIGHT_ON) == 1;
            mainLightComp.intensity = PlayerPrefs.GetFloat(LIGHT_INTENSITY);
            mainCamera.fieldOfView = PlayerPrefs.GetFloat(CAM_FOV);

            float camPosX = PlayerPrefs.GetFloat(CAM_POS_X);
            float camPosY = PlayerPrefs.GetFloat(CAM_POS_Y);
            float camPosZ = PlayerPrefs.GetFloat(CAM_POS_Z);
            float camRotX = PlayerPrefs.GetFloat(CAM_ROT_X);
            float camRotY = PlayerPrefs.GetFloat(CAM_ROT_Y);
            float camRotZ = PlayerPrefs.GetFloat(CAM_ROT_Z);
            float camRotW = PlayerPrefs.GetFloat(CAM_ROT_W);

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
            float boardRotX = PlayerPrefs.GetFloat(BOARD_ROT_X);
            float boardRotY = PlayerPrefs.GetFloat(BOARD_ROT_Y);
            float boardRotZ = PlayerPrefs.GetFloat(BOARD_ROT_Z);
            float boardRotW = PlayerPrefs.GetFloat(BOARD_ROT_W);

            float sqA1RotX = PlayerPrefs.GetFloat(SQA1_ROT_X);
            float sqA1RotY = PlayerPrefs.GetFloat(SQA1_ROT_Y);
            float sqA1RotZ = PlayerPrefs.GetFloat(SQA1_ROT_Z);
            float sqA1RotW = PlayerPrefs.GetFloat(SQA1_ROT_W);

            float sqH8RotX = PlayerPrefs.GetFloat(SQH8_ROT_X);
            float sqH8RotY = PlayerPrefs.GetFloat(SQH8_ROT_Y);
            float sqH8RotZ = PlayerPrefs.GetFloat(SQH8_ROT_Z);
            float sqH8RotW = PlayerPrefs.GetFloat(SQH8_ROT_W);

            mainCamera.transform.position = new Vector3(camPosX, camPosY, camPosZ);
            mainCamera.transform.rotation = new Quaternion(camRotX, camRotY, camRotZ, camRotW);
            mainLight.transform.position = new Vector3(lightPosX, lightPosY, lightPosZ);
            mainLight.transform.rotation = new Quaternion(lightRotX, lightRotY, lightRotZ, lightRotW);
            board.transform.position = new Vector3(boardPosX, boardPosY, boardPosZ);
            board.transform.rotation = new Quaternion(boardRotX, boardRotY, boardRotZ, boardRotW);
            squareTextA1.transform.rotation = new Quaternion(sqA1RotX, sqA1RotY, sqA1RotZ, sqA1RotW);
            squareTextH8.transform.rotation = new Quaternion(sqH8RotX, sqH8RotY, sqH8RotZ, sqH8RotW);
        }
    }
}
