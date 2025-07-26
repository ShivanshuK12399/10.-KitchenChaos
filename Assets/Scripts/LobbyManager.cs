using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{

    public static LobbyManager Instance { get; private set; }


    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs    // A custom EventArgs class that holds list of lobbies found
    {
        public List<Unity.Services.Lobbies.Models.Lobby> lobbyList;
    }

    public bool useRelay;

    private Unity.Services.Lobbies.Models.Lobby joinedLobby;
    private float heartbeatTimer;
    private float listLobbiesTimer;


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized) // initializing only once
        {
            // when working in editor we need diferent profiles to work on same device
            // so we are using random numbers to initialize
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions); // connects app to Unity's backend (for Auth, Relay)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Update()
    {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && AuthenticationService.Instance.IsSignedIn && 
            SceneManager.GetActiveScene().name==Loader.Scene.MainMenuScene.ToString()) // only list lobby if we are in mainmenu
        {
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f)
            {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 10f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions filterLobbies = new QueryLobbiesOptions
            {
                // Only get lobbies where available slots > 0 (i.e they are joinable)
                Filters = new List<QueryFilter>
                {
                  new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) // GT means greator than
                }
            };

            QueryLobbiesOptions allLobbies = new QueryLobbiesOptions(); // show all lobbies without any filter


            // Gets the list of matching lobbies from Unity's server.
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(allLobbies);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
            {
                lobbyList = queryResponse.Results
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async Task<Allocation> AllocateRelay()  // creates a Relay allocation to enable multiplayer networking using Unity Relay.
    {
        try
        {
            // CreateAllocationAsync(): Requests Unity’s Relay service to create a server allocation for the specified number of players.
            // maxPlayers - 1: Relay host doesn't count themselves, so we allocate for other players only.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(KitchenGameMultiplayer.Instance.maxPlayers - 1);

            // Returns: An Allocation object containing connection data (used for joining and hosting).
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation) 
    {
        try
        {
            // calls Unity's Relay Service to generate a unique join code for the session
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            // joinAllocation - Stores details about the joined Relay session like relay server address
            // await - Ensures the method waits for the Relay allocation to complete before continuing
            // joinAllocationSync - Calls Unity Relay Service to join a game session
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.Instance.maxPlayers, new CreateLobbyOptions
            {                                               // Calls Unity's Lobby Service to create a new lobby 
                IsPrivate = isPrivate,
            });

            if (useRelay)
            {
                // This requests Unity Relay to create a relay server allocation
                Allocation allocation = await AllocateRelay();

                string relayJoinCode = await GetRelayJoinCode(allocation);


                // Share the Relay join code with clients via the lobby, so client can fetch it and connect to the host.
                await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> // DataObject: The actual data stored.
                    {
                    // VisibilityOptions.Member: Only lobby members can see this data.
                    // relayJoinCode: The Relay join code to be stored.
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
                });

                // This tells the Unity Netcode transport layer to use the Relay server.
                // SetRelayServerData(...) injects the relay config into Unity's transport system.
                // creates a RelayServerData object that contains all necessary connection details.
                // dtls is protocal used for secure connection
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
            }  // Relay Code

            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void QuickJoin()
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (useRelay)
            {
                string code = joinedLobby.Data["RelayJoinCode"].Value;
                JoinAllocation joinAllocation = await JoinRelay(code);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            }  // Relay Code

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }  // Joins the public lobby

    public async void JoinWithId(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            if (useRelay)
            {
                string code = joinedLobby.Data["RelayJoinCode"].Value;
                JoinAllocation joinAllocation = await JoinRelay(code);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            }  // Relay Code

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }  // Joins the lobby which are listed in panel

    public async void JoinWithCode(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            if (useRelay)
            {
                string code = joinedLobby.Data["RelayJoinCode"].Value;
                JoinAllocation joinAllocation = await JoinRelay(code);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            }  // Relay Code

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }  // Joins the lobby which are private by code

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {                                                          // Removes this player from lobby
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public Unity.Services.Lobbies.Models.Lobby GetLobby()
    {
        return joinedLobby;
    }

}