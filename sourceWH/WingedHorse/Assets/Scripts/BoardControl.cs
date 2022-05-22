using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BoardControl : MonoBehaviour
{
    GameObject board;
    GameObject squareTextA1;
    GameObject squareTextH8;
    TextMeshPro whiteClock;
    TextMeshPro blackClock;
    bool isWhiteOnBottom = true;

    // Start is called before the first frame update
    void Start()
    {
        board = GameObject.Find("Board");
        squareTextA1 = GameObject.Find("Board/Ranks/Rank1/SquareText");
        squareTextH8 = GameObject.Find("Board/Ranks/Rank8/SquareText");
        whiteClock = GameObject.Find("Board/WhiteClock").GetComponent<TextMeshPro>();
        blackClock = GameObject.Find("Board/BlackClock").GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        whiteClock.text = MessageHandler.WhiteClock;
        blackClock.text = MessageHandler.BlackClock;

        if (isWhiteOnBottom != MessageHandler.IsRemoteWhiteOnBottom)
        {
            isWhiteOnBottom = MessageHandler.IsRemoteWhiteOnBottom;
            board.transform.Rotate(0, 180f, 0);
            squareTextA1.transform.Rotate(0, 0, 180f);
            squareTextH8.transform.Rotate(0, 0, 180f);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            board.transform.Rotate(0, -30f * Time.deltaTime, 0);
            squareTextA1.transform.Rotate(0, 0, -10f * Time.deltaTime);
            squareTextH8.transform.Rotate(0, 0, -10f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            board.transform.Rotate(0, 30f * Time.deltaTime, 0);
            squareTextA1.transform.Rotate(0, 0, 10f * Time.deltaTime);
            squareTextH8.transform.Rotate(0, 0, 10f * Time.deltaTime);
        }
    }
}
