using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
using UnityEngine;

public class CharacterSelectionUI : NetworkBehaviour
{

    public static CharacterSelectionUI instance;
    public event EventHandler OnReadyChange;
    public TextMeshProUGUI lobbyNameText, joinCodeText;

    private Dictionary<ulong, bool> playerReadyDict = new Dictionary<ulong, bool>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Lobby lobby=LobbyManager.Instance.GetLobby();

        lobbyNameText.text = lobby.Name;
        joinCodeText.text = lobby.LobbyCode;
    }


    [ServerRpc(RequireOwnership = false)]
    void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var senderID = serverRpcParams.Receive.SenderClientId;

        SetPlayerReadyClientRpc(senderID);
        playerReadyDict[senderID] = true;

        bool allPlayerReady = true;
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDict.ContainsKey(id) || !playerReadyDict[id])
            {
                allPlayerReady = false;
                break;
            }
        }

        if (allPlayerReady)
        {
            LobbyManager.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }


    [ClientRpc]
    void SetPlayerReadyClientRpc(ulong clientId)
    {
        playerReadyDict[clientId] = true;
        OnReadyChange?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDict.ContainsKey(clientId) && playerReadyDict[clientId];
    }

    public void Ready()
    {
        SetPlayerReadyServerRpc();
    }

    public void MainMenu()
    {
        LobbyManager.Instance.LeaveLobby();
        NetworkManager.Singleton.Shutdown();
        Loader.Load(Loader.Scene.MainMenuScene);

        if (KitchenGameMultiplayer.Instance != null)
        {
            Destroy(KitchenGameMultiplayer.Instance.gameObject);
        }
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}
