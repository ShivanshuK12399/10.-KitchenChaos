using Newtonsoft.Json.Bson;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public Button createPublicLobbyBtn, createPrivateLobbyBtn , joinLobbyBtn,joinCodeBtn;
    public TextMeshProUGUI statusText;
    public TMP_InputField lobbyNameInputField, joinCodeInputField;
    public Transform lobbyContainer, lobbyTemplate;

    [SerializeField] KitchenGameMultiplayer kitchenGameMultiplayer;


    void Start()
    {
        #region Buttons

        createPublicLobbyBtn.onClick.AddListener(() =>
        {
            if (lobbyNameInputField.text != "") LobbyManager.Instance.CreateLobby(lobbyNameInputField.text, false);
        });

        createPrivateLobbyBtn.onClick.AddListener(() =>
        {
            if (lobbyNameInputField.text != "") LobbyManager.Instance.CreateLobby(lobbyNameInputField.text, true);
        });

        joinLobbyBtn.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            LobbyManager.Instance.QuickJoin();
        }));

        joinCodeBtn.onClick.AddListener(() =>
        {
            if(joinCodeInputField.text != "") LobbyManager.Instance.JoinWithCode(joinCodeInputField.text);
        });
        #endregion

        KitchenGameMultiplayer.Instance.OnFailedTojoinGame += Instance_OnFailedTojoinGame;
        LobbyManager.Instance.OnCreateLobbyStarted += Instance_OnCreateLobbyStarted;
        LobbyManager.Instance.OnCreateLobbyFailed += Instance_OnCreateLobbyFailed;
        LobbyManager.Instance.OnJoinStarted += Instance_OnJoinStarted;
        LobbyManager.Instance.OnJoinFailed += Instance_OnJoinFailed;
        LobbyManager.Instance.OnQuickJoinFailed += Instance_OnQuickJoinFailed;

        LobbyManager.Instance.OnLobbyListChanged += Instance_OnLobbyListChanged;
        UpdateLobbyList(new List<Lobby>());
    }

    #region messages
    private void Instance_OnCreateLobbyStarted(object sender, System.EventArgs e)
    {
        print("Creating Lobby...");
        statusText.text = "Creating Lobby...";
    }

    private void Instance_OnCreateLobbyFailed(object sender, System.EventArgs e)
    {
        statusText.text = "Failed to Create Lobby";
    }

    private void Instance_OnJoinStarted(object sender, System.EventArgs e)
    {
        statusText.text = "Joining Lobby...";
    }

    private void Instance_OnJoinFailed(object sender, System.EventArgs e)
    {
        statusText.text = "Failed to Join Lobby";
    }

    private void Instance_OnQuickJoinFailed(object sender, System.EventArgs e)
    {
        statusText.text = "Could not find any Lobby to Join !!";
    }
    #endregion

    void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach(Transform obj in lobbyContainer)
        {
            Destroy(obj.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
           Transform template= Instantiate(lobbyTemplate, lobbyContainer);
            template.GetComponent<LobbyTemplate>().SetLobby(lobby);
        }
    }

    private void Instance_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void Instance_OnFailedTojoinGame(object sender, System.EventArgs e)
    {
        if (NetworkManager.Singleton.DisconnectReason == "") statusText.text = "Failed to connect";
        else statusText.text = NetworkManager.Singleton.DisconnectReason;
    }

    private void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnFailedTojoinGame -= Instance_OnFailedTojoinGame;
        LobbyManager.Instance.OnCreateLobbyStarted -= Instance_OnCreateLobbyStarted;
        LobbyManager.Instance.OnCreateLobbyFailed -= Instance_OnCreateLobbyFailed;
        LobbyManager.Instance.OnJoinStarted -= Instance_OnJoinStarted;
        LobbyManager.Instance.OnJoinFailed -= Instance_OnJoinFailed;
        LobbyManager.Instance.OnQuickJoinFailed -= Instance_OnQuickJoinFailed;
        LobbyManager.Instance.OnLobbyListChanged -= Instance_OnLobbyListChanged;
    }
}
