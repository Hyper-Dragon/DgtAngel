using TMPro;
using UnityEngine;

public class BoardControl : MonoBehaviour
{
    private GameObject board;
    private GameObject squareTextA1;
    private GameObject squareTextH8;
    private TextMeshPro whiteClock;
    private TextMeshPro blackClock;
    private TextMeshPro whiteText;
    private TextMeshPro blackText;
    private bool isWhiteOnBottom = true;

    public float rotationStep = 5f;
    public float speedUpModifier = 10f;

    // Start is called before the first frame update
    void Start()
    {
        board = GameObject.Find("Board");
        squareTextA1 = GameObject.Find("Board/Ranks/Rank1/SquareText");
        squareTextH8 = GameObject.Find("Board/Ranks/Rank8/SquareText");
        whiteClock = GameObject.Find("Board/WhiteClock").GetComponent<TextMeshPro>();
        blackClock = GameObject.Find("Board/BlackClock").GetComponent<TextMeshPro>();
        whiteText = GameObject.Find("Board/WhiteText").GetComponent<TextMeshPro>();
        blackText = GameObject.Find("Board/BlackText").GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        float speedModifier = Input.GetKey(KeyCode.Space) ? speedUpModifier : 1f;
        
        whiteClock.text = MessageHandler.WhiteClock;
        blackClock.text = MessageHandler.BlackClock;
        whiteText.text = MessageHandler.MessageText;
        blackText.text = MessageHandler.MessageText;

        if (isWhiteOnBottom != MessageHandler.IsRemoteWhiteOnBottom)
        {
            isWhiteOnBottom = MessageHandler.IsRemoteWhiteOnBottom;
            board.transform.Rotate(0, 180f, 0);
            squareTextA1.transform.Rotate(0, 0, 180f);
            squareTextH8.transform.Rotate(0, 0, 180f);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            board.transform.Rotate(0, -rotationStep * speedModifier * Time.deltaTime, 0);
            squareTextA1.transform.Rotate(0, 0, -rotationStep * speedModifier * Time.deltaTime);
            squareTextH8.transform.Rotate(0, 0, -rotationStep * speedModifier * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            board.transform.Rotate(0, rotationStep * speedModifier * Time.deltaTime, 0);
            squareTextA1.transform.Rotate(0, 0, rotationStep * speedModifier * Time.deltaTime);
            squareTextH8.transform.Rotate(0, 0, rotationStep * speedModifier * Time.deltaTime);
        }
    }
}
