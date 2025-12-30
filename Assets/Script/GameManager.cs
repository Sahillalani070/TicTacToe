using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }
    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }
    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }
    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance!");
        }
        Instance = this;
        playerTypeArray = new PlayerType[3, 3];
        lineList = new List<Line>
        {
            //Horizontal lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),},
                centerGridPosition = new Vector2Int(1,0),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(1,2),
                orientation = Orientation.Horizontal,
            },
            //Vertical Lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),},
                centerGridPosition = new Vector2Int(0,1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(2,1),
                orientation = Orientation.Vertical,
            },
            //Diagonal Lines
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,0), new Vector2Int(1,1), new Vector2Int(2,2),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>{new Vector2Int(0,2), new Vector2Int(1,1), new Vector2Int(2,0),},
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB,
            },
        };
    }
    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn:" + NetworkManager.Singleton.LocalClientId);
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            localPlayerType = PlayerType.Cross;
        }
        else
        {
            localPlayerType = PlayerType.Circle;
        }
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnecetedCallback;
        }
        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayertype, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };
    }
    private void NetworkManager_OnClientConnecetedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            // Start Game
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }
    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log("CLickedOnGridPosition" + x + "," + y);
        if (playerType != currentPlayablePlayerType.Value)
        {
            return;
        }
        if (playerTypeArray[x, y] != PlayerType.None)
        {
            //Already Occupied
            return;
        }
        playerTypeArray[x, y] = playerType;
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        });
        switch (currentPlayablePlayerType.Value)
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;

        }
        TestWinner();
    }
    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
        );
    }
    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bplayerType, PlayerType cplayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bplayerType &&
            bplayerType == cplayerType;
    }
    private void TestWinner()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];
            if (TestWinnerLine(line))
            {
                //Win!
                Debug.Log("Winner!");
                currentPlayablePlayerType.Value = PlayerType.None;
                TriggerOnGameWinRpc(i, playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y]);
                // switch (winPlayerType)
                // {
                //     case PlayerType.Cross:
                //         playerCrossScore.Value++;
                //         break;
                //     case PlayerType.Circle:
                //         playerCircleScore.Value++;
                //         break;
                // }
                // TriggerOnGameWinRpc(i, winPlayerType);
                TriggerOnGameWinRpc(i, playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y]);
                return;
            }
        }
        bool hasTie = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x, y] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }
        }
        if (hasTie)
        {
            TriggerOnGameTiedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, OnGameWinEventArgs.Empty);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType,
        });
    }
    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }
        currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }
    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType.Value;
    }
}
