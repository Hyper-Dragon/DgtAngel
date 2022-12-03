using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightArray : MonoBehaviour
{
    private string[] lastMessageProcessed = new string[0];
    private readonly Dictionary<string, (Renderer spot, Renderer square)> meshDictionary = new();

    // Start is called before the first frame update
    void Awake()
    {
        //Prebuild required mesh references
        Array.ForEach(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" },
                      rank => Array.ForEach(new string[] { "A", "B", "C", "D", "E", "F", "G", "H" },
                      file => meshDictionary.Add($"{file}{rank}",
                               (spot: GameObject
                                      .Find($"Board/RanksSpot/Rank{rank}/Square{file}")
                                      .GetComponentInChildren<Renderer>(),
                                square: GameObject
                                        .Find($"Board/Ranks/Rank{rank}/Square{file}")
                                        .GetComponentInChildren<Renderer>()))));
    }

    // Update is called once per frame
    void Update()
    {
        //Check for any changes and process
        if (MessageHandler.SquareDiffQueue.TryDequeue(out string message))
        {
            //Turn off old active squares
            Array.ForEach(lastMessageProcessed, 
                          square => { meshDictionary[square].spot.enabled = false;
                                      meshDictionary[square].square.enabled = true;});

            //Extract squares from message making sure the key is in the dictionary
            lastMessageProcessed = message.Split(',').Where(x => meshDictionary.ContainsKey(x)).ToArray();

            //Turn on new active squares
            Array.ForEach(lastMessageProcessed,
                          square => {
                              meshDictionary[square].spot.enabled = true;
                              meshDictionary[square].square.enabled = false;});
        }
    }
}
