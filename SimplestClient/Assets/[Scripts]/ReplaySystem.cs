using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplaySystem : MonoBehaviour
{

    [SerializeField] 
    public int[,] TicTacToeGameReplay;

    public Queue<MovesOrderClass> movesOrder;

    public static ReplaySystem _instance;


    public static ReplaySystem GetInstance()
    {
        return _instance;
    }

    // Start is called before the first frame update
    void Start()
    {
        movesOrder = new Queue<MovesOrderClass>();
        _instance = this;
    }

    private void OnEnable()
    {
        TicTacToeGameReplay = new int[3, 3];
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log(movesOrder.Count);
            MovesOrderClass movesOrderTemp = new MovesOrderClass();

            while (movesOrder.Count > 0)
            {
                movesOrderTemp = movesOrder.Dequeue();
                Debug.Log("Player" + movesOrderTemp.player + "\tMovePosition = " + movesOrderTemp.moveLocation.x + "," + movesOrderTemp.moveLocation.y);
            }
        }
    }

    public class MovesOrderClass
    {
        public Vector2Int moveLocation;
        public int player;

        public MovesOrderClass()
        { }

        public MovesOrderClass(Vector2Int mL, int Player)
        {
            moveLocation = mL;
            player = Player;
        }
    }
}
