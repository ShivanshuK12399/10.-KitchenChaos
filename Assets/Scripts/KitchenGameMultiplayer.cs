using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameMultiplayer : NetworkBehaviour
{
    public static KitchenGameMultiplayer Instance { get; private set; }

    public event EventHandler OnFailedTojoinGame;
    public event EventHandler OnPlayerDataListChange;

    public int maxPlayers = 4;
    public bool DirectOpeningGamescene;
    public KitchenObjectListSO kitchenObjectListSO;
    public List<Color> playerColorList;

    public NetworkList<PlayerData> playerDataNetworkList; // can't be initialized here or in onSpawn
    public string joinCodeText;

    public static bool playMultiplayer=true;


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    public void StartSinglePlayer()
    {
        StartHost();
        Loader.LoadNetwork(Loader.Scene.GameScene);
    }
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataListChange?.Invoke(this,EventArgs.Empty);
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (DirectOpeningGamescene) // Directly opening game scene for developing
        {
            // Not allowing late joining
            if (KitchenGameManager.Instance.IsWaitingToStart())
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
            else response.Approved = false;
        }
        else // Opening from main menu for complete game test
        {
            if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString())
            {
                response.Approved = false;
                response.Reason = "Game is started";
                return;
            }
            if (NetworkManager.Singleton.ConnectedClientsList.Count >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Game is full";
                return;
            }
            response.Approved = true;
            response.Pending = false;
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong id)
    {
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = id,
            colorId = GetUnusedColorId()
        });
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong id)
    {
        for (int i = 0;i< playerDataNetworkList.Count; i++)
        {
            var playerData= playerDataNetworkList[i];
            if (playerData.clientId == id)
            {
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    private void Singleton_Client_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedTojoinGame?.Invoke(this, EventArgs.Empty);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient() 
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartClient();
    }

    #region multiplayerCode

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectListSO.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectListSO[kitchenObjectSOIndex];
    }



    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        ClearKitchenObjectOnParentClientRpc(kitchenObjectNetworkObjectReference);

        kitchenObject.DestroySelf();
    }

    [ClientRpc]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }
    #endregion

    #region Sending Data
    public bool IsPlayerConnected(int index)
    {
        return index < playerDataNetworkList.Count;
    }

    public int GetPlayerIndex(ulong id)  // getting local player index from playerNetworkList
    {
        for(int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if(playerDataNetworkList[i].clientId==id) return i;
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(ulong id)
    {
        foreach (var playerData in playerDataNetworkList)
        {
            if (playerData.clientId == id)
            {
                return playerData;
            }
        }
        return default;
    }
    public PlayerData GetPlayerData()  // getting local player from playerNetworkList
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromIndex(int index)
    {
        return playerDataNetworkList[index];
    }

    public Color GetPlayerColor(int colorId)
    {
        return playerColorList[colorId];
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership =false)]
    void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverParams=default)
    {                                                     // serverparams refers to client which call this function
        if (IsColorAvailable(colorId))
        {
            int playerIndex = GetPlayerIndex(serverParams.Receive.SenderClientId);
            var playerData= GetPlayerDataFromIndex(playerIndex);

            playerData.colorId = colorId;
            playerDataNetworkList[playerIndex]= playerData;
        }
    }

    bool IsColorAvailable(int colorId)
    {
        foreach(var playerData in playerDataNetworkList)
        {
            if (playerData.colorId==colorId)
            {
                return false;
            }
        }
        return true;
    }

    int GetUnusedColorId()
    {
        for (int i = 0;i<playerColorList.Count;i++)
        {
            if (IsColorAvailable(i))
            {
                return i;
            }
        }
        return -1;
    }
    #endregion
}