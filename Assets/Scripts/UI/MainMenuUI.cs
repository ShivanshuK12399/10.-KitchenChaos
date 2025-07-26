using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour 
{

    [SerializeField] private Button playButton;
    [SerializeField] private Button SinglePlayButton;
    [SerializeField] private Button quitButton;


    private void Awake()
    {
        /*if (KitchenGameMultiplayer.Instance != null)
        {
            Destroy(KitchenGameMultiplayer.Instance.gameObject);
        }
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
        if (Lobby.Instance != null)
        {
            Destroy(Lobby.Instance.gameObject);
        }*/
    }

    private void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            KitchenGameMultiplayer.playMultiplayer = true;
            //Loader.Load(Loader.Scene.LobbyScene);
        });
        SinglePlayButton.onClick.AddListener(() =>
        {
            KitchenGameMultiplayer.Instance.StartSinglePlayer();
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}