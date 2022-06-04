using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiltCollision : MonoBehaviour
{
    public bool isMoveOk = true;
    
    private void OnTriggerEnter(Collider other)
    {
        isMoveOk = false;

        string objectName = $"{other.transform.parent.gameObject.name}{other.name}";
        print($"Collision {objectName}");
    }

    private void OnTriggerExit(Collider other)
    {
        isMoveOk = true;
        
        string objectName = $"{other.transform.parent.gameObject.name}{other.name}";
        print($"Collision {objectName}");
    }
}
