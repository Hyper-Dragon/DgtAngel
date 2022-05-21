using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightArray : MonoBehaviour
{

    private string lastMessageProcessed="";
    private readonly Dictionary<string, Renderer> spotMeshDictionary = new();
    private readonly Dictionary<string, Renderer> squareMeshDictionary = new();

    // Start is called before the first frame update
    void Start()
    {

        foreach (var rank in new string[] { "1", "2", "3", "4", "5", "6", "7", "8" })
        {
            foreach (var file in new string[] { "A", "B", "C", "D", "E", "F", "G", "H" })
            {
                spotMeshDictionary
                    .Add($"{file}{rank}", GameObject.Find($"Board/RanksSpot/Rank{rank}/Square{file}")
                    .GetComponentInChildren<Renderer>());
                squareMeshDictionary
                    .Add($"{file}{rank}", GameObject.Find($"Board/Ranks/Rank{rank}/Square{file}")
                    .GetComponentInChildren<Renderer>());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Check for changes and process
        if (MessageHandler.SquareDiffQueue.TryDequeue(out string message))
        {
            //Turn off old active squares
            foreach (var square in lastMessageProcessed.Split(','))
            {
                if (spotMeshDictionary.ContainsKey(square))
                {
                    spotMeshDictionary[square].enabled = false;
                    squareMeshDictionary[square].enabled = true;
                }
            }

            //Turn on new active squares
            lastMessageProcessed = message;
            foreach (var square in lastMessageProcessed.Split(','))
            {
                spotMeshDictionary[square].enabled = true;
                squareMeshDictionary[square].enabled = false;
            }
        }
    }
}
