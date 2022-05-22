using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //This is Main Camera in the Scene
    private Camera mainCamera;
    private GameObject mainLight;
    private GameObject board;
    GameObject squareText;
    private Vector3 cameraTargetPosition;
    private Vector3 tableCentre;
    private Vector3 initialCameraPos = new(0, 0, 0);
    private Quaternion initialCameraRot = new(0, 0, 0, 0);
    private bool isLastMoveLeft = false;
    private bool isLastMoveUp = false;
    
    public int targetFrameRate = 30;

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        mainCamera = Camera.main;
        mainCamera.enabled = true;

        mainLight = GameObject.Find("MainLight");

        board = GameObject.Find("Board");
        cameraTargetPosition = GameObject.Find("CameraTarget").transform.position;
        tableCentre = board.transform.position;

        initialCameraPos = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,
                                       mainCamera.transform.position.z);
        initialCameraRot = new Quaternion(mainCamera.transform.rotation.x, mainCamera.transform.rotation.y,
                                          mainCamera.transform.rotation.z, mainCamera.transform.rotation.w);
    }


    // Update is called once per frame
    void Update()
    {
        float speedModifier = Input.GetKey(KeyCode.LeftAlt) ? 10f : 1f;

        if (Input.GetKey(KeyCode.Space))
        {
            mainCamera.transform.SetPositionAndRotation(initialCameraPos, initialCameraRot);
        }

        Vector3 viewPos = mainCamera.WorldToViewportPoint(board.transform.position);
        bool isLeftOk = true;
        bool isRightOk = true;
        bool isZoomInOk = true;
        bool isZoomOutOk = true;
        bool isUpOk = true;
        bool isDownOk = true;

        
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

            if (viewPos.x <= 0.5)
            {
                isUpOk = true;
                isDownOk = false;
            }
            else
            {
                isUpOk = false;
                isDownOk = true;
            }

        }

        if (Input.GetKey(KeyCode.D) && isLeftOk)
        {
            board.transform.position += new Vector3(0, 0, 2f * speedModifier * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.A) && isRightOk)
        {
            board.transform.position += new Vector3(0, 0, -2f * speedModifier * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) && isUpOk)
        {
            board.transform.position += new Vector3(-2f * speedModifier * Time.deltaTime, 0, 0);
        }
        else if (Input.GetKey(KeyCode.S) && isDownOk)
        {
            board.transform.position += new Vector3(2f * speedModifier * Time.deltaTime, 0, 0);
        }


        if (Input.GetKey(KeyCode.Z) && mainCamera.fieldOfView > 20 && isZoomInOk)
        {
            mainCamera.fieldOfView -= 5 * speedModifier * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.X) && mainCamera.fieldOfView < 70 && isZoomOutOk)
        {
            mainCamera.fieldOfView += 5f * speedModifier * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (mainCamera.transform.eulerAngles.x >= 25 || !isLastMoveUp)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, 15 * Time.deltaTime), 20 * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, 15 * Time.deltaTime), 20 * speedModifier * Time.deltaTime);
                isLastMoveUp = true;
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            if (mainCamera.transform.eulerAngles.x >= 25 || isLastMoveUp)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -15 * Time.deltaTime), 20 * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -15 * Time.deltaTime), 20 * speedModifier * Time.deltaTime);
                isLastMoveUp = false;
            }
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (mainCamera.transform.eulerAngles.x >= 25 || !isLastMoveLeft)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(15 * Time.deltaTime, 0, 0), 20 * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(15 *Time.deltaTime, 0, 0), 20 * speedModifier * Time.deltaTime);
                isLastMoveLeft = true;
            }
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            if (mainCamera.transform.eulerAngles.x >= 25 || isLastMoveLeft)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(-15 * Time.deltaTime, 0, 0), 20 * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(-15 * Time.deltaTime, 0, 0), 20 * speedModifier * Time.deltaTime);
                isLastMoveLeft = false;
            }
        }
    }
}