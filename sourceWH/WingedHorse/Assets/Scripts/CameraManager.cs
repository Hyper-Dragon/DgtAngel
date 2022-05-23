using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    //This is Main Camera in the Scene
    private Camera mainCamera;
    private GameObject mainLight;
    private GameObject board;
    private Renderer cameraTarget;
    private Rigidbody cameraBody;
    private Vector3 cameraTargetPosition;
    private Vector3 initialCameraPos = new(0, 0, 0);
    private Quaternion initialCameraRot = new(0, 0, 0, 0);

    
    public int targetFrameRate = 30;
    public float moveStep = 1f;
    public float fovStep = 2f;
    public float rotationStep = 10f;
    public float speedUpModifier = 10f;
    
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        mainCamera = Camera.main;
        mainCamera.enabled = true;

        mainLight = GameObject.Find("MainLight");

        board = GameObject.Find("Board");
        cameraTarget = GameObject.Find("Board/BaseTarget/Centre").GetComponent<Renderer>();
        cameraBody = GameObject.Find("Main Camera").GetComponent<Rigidbody>();
        cameraTargetPosition = GameObject.Find("CameraTarget").transform.position;

        initialCameraPos = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,
                                       mainCamera.transform.position.z);
        initialCameraRot = new Quaternion(mainCamera.transform.rotation.x, mainCamera.transform.rotation.y,
                                          mainCamera.transform.rotation.z, mainCamera.transform.rotation.w);
    }


    // Update is called once per frame
    void Update()
    {
        // Reset Scene
        if (Input.GetKey(KeyCode.R))
        {
            mainCamera.transform.SetPositionAndRotation(initialCameraPos, initialCameraRot);
        }

        float speedModifier = Input.GetKey(KeyCode.Space) ? speedUpModifier : 1f;
        board.transform.position += Input.GetKey(KeyCode.D) && (cameraTarget.isVisible || cameraTarget.transform.position.z < 0) ? new Vector3(0, 0, moveStep * speedModifier * Time.deltaTime) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.A) && (cameraTarget.isVisible || cameraTarget.transform.position.z > 0) ? new Vector3(0, 0, -moveStep * speedModifier * Time.deltaTime) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.W) && (cameraTarget.isVisible || cameraTarget.transform.position.x > 0) ? new Vector3(-moveStep * speedModifier * Time.deltaTime, 0, 0) : Vector3.zero;
        board.transform.position += Input.GetKey(KeyCode.S) && (cameraTarget.isVisible || cameraTarget.transform.position.x < 0) ? new Vector3(moveStep * speedModifier * Time.deltaTime, 0, 0) : Vector3.zero;
        mainCamera.fieldOfView -= Input.GetKey(KeyCode.Z) && (cameraTarget.isVisible && mainCamera.fieldOfView > 20) ? fovStep * speedModifier * Time.deltaTime : 0;
        mainCamera.fieldOfView += Input.GetKey(KeyCode.X) && (cameraTarget.isVisible && mainCamera.fieldOfView < 70) ? fovStep * speedModifier * Time.deltaTime : 0;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (mainCamera.transform.forward.x < 0.5)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            if (mainCamera.transform.forward.x > -0.5)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(0, 0, -rotationStep * Time.deltaTime), rotationStep * speedModifier * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (mainCamera.transform.forward.z > -0.5)
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
            }
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            if (mainCamera.transform.forward.z < 0.5 )
            {
                mainCamera.transform.RotateAround(cameraTargetPosition, new Vector3(-rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
                mainLight.transform.RotateAround(cameraTargetPosition, new Vector3(-rotationStep * Time.deltaTime, 0, 0), rotationStep * speedModifier * Time.deltaTime);
            }
        }
    }

}