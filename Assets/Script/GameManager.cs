using System;
using UnityEngine;
using Unity.Netcode;
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
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }

    private PlayerType localPlayerType;
    private PlayerType currentPlayablePlayerType;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance!");
        }
        Instance = this;
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
    }
    private void NetworkManager_OnClientConnecetedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            // Start Game
            currentPlayablePlayerType = PlayerType.Cross;
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
        if (playerType != currentPlayablePlayerType)
        {
            return;
        }
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType,
        });
        switch (currentPlayablePlayerType)
        {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType = PlayerType.Cross;
                break;

        }
        TriggerOnCurrentPlayablePlayerTypeChangedRPC();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnCurrentPlayablePlayerTypeChangedRPC()
    {
        OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType()
    {
        return localPlayerType;
    }
    public PlayerType GetCurrentPlayablePlayerType()
    {
        return currentPlayablePlayerType;
    }
}
