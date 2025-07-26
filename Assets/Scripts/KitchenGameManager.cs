using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenGameManager : NetworkBehaviour {


    public static KitchenGameManager Instance { get; private set; }


    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    public event EventHandler OnLocalPlayerReadyChanged;

    private NetworkVariable< State> state=new NetworkVariable<State>(State.WaitingToStart);
    private NetworkVariable< float> countdownToStartTimer =new NetworkVariable<float>(3f);
    private NetworkVariable< float> gamePlayingTimer=new NetworkVariable<float>(0);
    private Dictionary<ulong,bool> playerReadyDict = new Dictionary<ulong,bool>();

    public Transform playerPrefab;
    public float GameDuration = 90f;
    private bool isGamePaused = false, isLocalplayerReady = false;


    private enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }

    private void Awake()
    {
        Instance = this;
    }


    void Start() 
    {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += State_OnValueChanged;

        if(IsServer)
        {
            // Spawns player after loading a scene
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += NetworkManager_OnLoadEventCompleted;
        }
    }

    void NetworkManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform prefab = Instantiate(playerPrefab);
            prefab.GetComponent<NetworkObject>().SpawnAsPlayerObject(client, true);
        }
    }

    void State_OnValueChanged(State prevValue,State newValue)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        if (!IsServer) return;


        switch (state.Value)
        {
            case State.WaitingToStart:
                break;

            case State.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = GameDuration;
                }
                break;

            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    state.Value = State.GameOver;
                }
                break;

            case State.GameOver:
                break;
        }
    }


    void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        Interact();
    }

    public void Interact() // also calling this function for game input to set active player in tutorial button
    {
        if (state.Value == State.WaitingToStart)
        {
            isLocalplayerReady = true;

            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            SetPlayerReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership =false)]
    void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams=default)
    {
        var senderID = serverRpcParams.Receive.SenderClientId;
        playerReadyDict[senderID] = true;

        bool allPlayerReady = true;
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDict.ContainsKey(id)|| !playerReadyDict[id])
            {
                allPlayerReady = false;
                break;
            }
        }

        if(allPlayerReady) state.Value = State.CountdownToStart;
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        if (isGamePaused)
        {
            //Time.timeScale = 0;

            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Time.timeScale = 1;

            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    #region Return Data

    public bool IsGamePlaying() {
        return state.Value == State.GamePlaying;
    }

    public bool IsCountdownToStartActive() {
        return state.Value == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer() {
        return countdownToStartTimer.Value;
    }

    public bool IsGameOver() {
        return state.Value == State.GameOver;
    }

    public float GetGamePlayingTimerNormalized() {
        return 1 - (gamePlayingTimer.Value / GameDuration);
    }

    public bool IsWaitingToStart()
    {
        return state.Value == State.WaitingToStart;
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalplayerReady = true;
    }

    #endregion

}