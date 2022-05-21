using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //This is Main Camera in the Scene
    private Camera mainCamera;
    private GameObject board;
    GameObject squareText;
    private Vector3 initialCameraPos = new(0, 0, 0);
    private Quaternion initialCameraRot = new(0, 0, 0, 0);

    public int targetFrameRate = 30;

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        mainCamera = Camera.main;
        mainCamera.enabled = true;

        board = GameObject.Find("Board");

        initialCameraPos = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,
                                       mainCamera.transform.position.z);
        initialCameraRot = new Quaternion(mainCamera.transform.rotation.x, mainCamera.transform.rotation.y,
                                          mainCamera.transform.rotation.z, mainCamera.transform.rotation.w);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            mainCamera.transform.SetPositionAndRotation(initialCameraPos, initialCameraRot);
        }

        Vector3 viewPos = mainCamera.WorldToViewportPoint(board.transform.position);
        bool isLeftOk = true;
        bool isRightOk = true;
        bool isZoomInOk = true;
        bool isZoomOutOk = true;

        if (viewPos.x <= 0 || viewPos.x >= 1 ||
            viewPos.y <= 0 || viewPos.y >= 1 ||
            viewPos.z <= 0)
        {
            if (viewPos.x <= 0.5)
            {
                isLeftOk = true;
                isRightOk = false;
                isZoomInOk = false;
                isZoomOutOk = true;
            }
            else
            {
                isLeftOk = false;
                isRightOk = true;
                isZoomInOk = false;
                isZoomOutOk = true;
            }
        }
        

        if (Input.GetKey(KeyCode.D) && isLeftOk)
        {
            //print($"X::{viewPos.x} Y::{viewPos.y} Z::{viewPos.z}");
            // Camera or Board????
            board.transform.position += new Vector3(0, 0, 2f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.A) && isRightOk)
        {
            //print($"X::{viewPos.x} Y::{viewPos.y} Z::{viewPos.z}");
            // Camera or Board????
            board.transform.position += new Vector3(0, 0, -2f * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) && mainCamera.fieldOfView > 20 && isZoomInOk)
        {
            mainCamera.fieldOfView -= 5 * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.W) && mainCamera.fieldOfView < 70 && isZoomOutOk)
        {
            mainCamera.fieldOfView += 5f * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.UpArrow) && 
            mainCamera.transform.eulerAngles.x > 50 )
        {
            mainCamera.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 0, 15 * Time.deltaTime), 40 * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.DownArrow) && 
                 mainCamera.transform.eulerAngles.x > 50)
        {
            mainCamera.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(0, 0, -15 * Time.deltaTime), 40 * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.LeftArrow) &&
            mainCamera.transform.eulerAngles.x > 50)
        {
            mainCamera.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(15 * Time.deltaTime, 0, 0), 40 * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow) &&
            mainCamera.transform.eulerAngles.x > 50)
        {
            mainCamera.transform.RotateAround(new Vector3(0, 0, 0), new Vector3(-15 * Time.deltaTime, 0, 0), 40 * Time.deltaTime);
        }
    }
}