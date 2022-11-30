using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //This is Main Camera in the Scene
    public Camera mainCamera;
    
    private GameObject mainLight;
    private GameObject board;
    private Renderer cameraTarget;
    private Rigidbody cameraBody;
    private Vector3 cameraTargetPosition;
    private Vector3 initialCameraPos = new(0, 0, 0);
    private Quaternion initialCameraRot = new(0, 0, 0, 0);
    private Vector3 initialLightPos = new(0, 0, 0);
    private Quaternion initialLightRot = new(0, 0, 0, 0);
    private Vector3 initialBoardPos = new(0, 0, 0);

    private TiltCollision floorTiltLeft;
    private TiltCollision floorTiltRight;
    private TiltCollision floorTiltUp;
    private TiltCollision floorTiltDown;
    private TiltCollision ceilingTiltLeft;
    private TiltCollision ceilingTiltRight;
    private TiltCollision ceilingTiltUp;
    private TiltCollision ceilingTiltDown;

    public int targetFrameRate = 30;
    public float moveStep = 1f;
    public float fovStep = 2f;
    public float rotationStep = 10f;
    public float speedUpModifier = 10f;
    
    // Start is called before the first frame update
    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        mainCamera = Camera.main;
        mainCamera.enabled = true;

        mainLight = GameObject.Find("MainLight");
        floorTiltLeft  = GameObject.Find("Main Camera/FloorColliderLeft").GetComponent<TiltCollision>();
        floorTiltRight = GameObject.Find("Main Camera/FloorColliderRight").GetComponent<TiltCollision>();
        floorTiltUp    = GameObject.Find("Main Camera/FloorColliderUp").GetComponent<TiltCollision>();
        floorTiltDown  = GameObject.Find("Main Camera/FloorColliderDown").GetComponent<TiltCollision>();
        ceilingTiltLeft = GameObject.Find("Main Camera/CeilingColliderLeft").GetComponent<TiltCollision>();
        ceilingTiltRight = GameObject.Find("Main Camera/CeilingColliderRight").GetComponent<TiltCollision>();
        ceilingTiltUp = GameObject.Find("Main Camera/CeilingColliderUp").GetComponent<TiltCollision>();
        ceilingTiltDown = GameObject.Find("Main Camera/CeilingColliderDown").GetComponent<TiltCollision>();
        
        board = GameObject.Find("Board");
        cameraTarget = GameObject.Find("Board/BaseTarget/Centre").GetComponent<Renderer>();
        cameraBody = GameObject.Find("Main Camera").GetComponent<Rigidbody>();
        cameraTargetPosition = GameObject.Find("CameraTarget").transform.position;

        initialCameraPos = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,
                                       mainCamera.transform.position.z);
        initialCameraRot = new Quaternion(mainCamera.transform.rotation.x, mainCamera.transform.rotation.y,
                                          mainCamera.transform.rotation.z, mainCamera.transform.rotation.w);
        initialLightPos = new Vector3(mainLight.transform.position.x, mainLight.transform.position.y,
                                       mainLight.transform.position.z);
        initialLightRot = new Quaternion(mainLight.transform.rotation.x, mainLight.transform.rotation.y,
                                          mainLight.transform.rotation.z, mainLight.transform.rotation.w);
        initialBoardPos = board.transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        // Reset Scene
        if (Input.GetKey(KeyCode.R))
        {
            mainCamera.transform.SetPositionAndRotation(initialCameraPos, initialCameraRot);
            mainLight.transform.SetPositionAndRotation(initialLightPos, initialLightRot);
            board.transform.position = initialBoardPos;
        }

        float speedModifier = Input.GetKey(KeyCode.Space) ? speedUpModifier : 1f;
        board.transform.position += Input.GetKey(KeyCode.D) && (cameraTarget.isVisible || cameraTarget.transform.position.z < 0) ? new Vector3(0, 0, moveStep * speedModifier * Time.deltaTime) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.A) && (cameraTarget.isVisible || cameraTarget.transform.position.z > 0) ? new Vector3(0, 0, -moveStep * speedModifier * Time.deltaTime) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.W) && (cameraTarget.isVisible || cameraTarget.transform.position.x > 0) ? new Vector3(-moveStep * speedModifier * Time.deltaTime, 0, 0) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.S) && (cameraTarget.isVisible || cameraTarget.transform.position.x < 0) ? new Vector3(moveStep * speedModifier * Time.deltaTime, 0, 0) : Vector3.zero;
        mainCamera.fieldOfView -= Input.GetKey(KeyCode.Z) && (cameraTarget.isVisible && mainCamera.fieldOfView > 20) ? fovStep * speedModifier * Time.deltaTime : 0;
        mainCamera.fieldOfView += Input.GetKey(KeyCode.X) && (cameraTarget.isVisible && mainCamera.fieldOfView < 70) ? fovStep * speedModifier * Time.deltaTime : 0;

        if (Input.GetKey(KeyCode.UpArrow) && floorTiltUp.isMoveOk && ceilingTiltUp.isMoveOk)
        {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.DownArrow) && floorTiltDown.isMoveOk && ceilingTiltDown.isMoveOk)
        {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.LeftArrow) && floorTiltLeft.isMoveOk && ceilingTiltLeft.isMoveOk)
        {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow) && floorTiltRight.isMoveOk && ceilingTiltRight.isMoveOk)
        {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(-rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(-rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
        }
    }
}