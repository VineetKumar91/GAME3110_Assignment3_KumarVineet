using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplaySystem : MonoBehaviour
{

    [SerializeField] 
    public int[,] TicTacToeGameReplay;

    // Queue data struct for storing moves IN order
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

    /// <summary>
    /// A class for storing move location AND which player
    /// </summary>
    public class MovesOrderClass
    {
        public Vector2Int moveLocation;
        public int player;

        /// <summary>
        /// def constructor
        /// </summary>
        public MovesOrderClass()
        { }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="mL"></param>
        /// <param name="Player"></param>
        public MovesOrderClass(Vector2Int mL, int Player)
        {
            moveLocation = mL;
            player = Player;
        }
    }
}
