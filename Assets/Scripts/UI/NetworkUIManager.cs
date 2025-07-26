using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject waitingPanel;
    public GameObject hostDisconnectPanel;
    public GameObject gameoverPanel;
    public GameObject tutorialPanel;
    public GameObject pausePanel;

    public TextMeshProUGUI HighestDishesDeliverdText, dishesDeliveredText;

    [Header("Buttons")]
    public Button hostBtn;
    public Button clientBtn;
    public Button playAgain;
    public GameObject mobileButtons;

    void Start()
    {

#if UNITY_ANDROID || UNITY_EDITOR
        mobileButtons.SetActive(true);
#endif

        hostBtn.onClick.AddListener(() =>{ KitchenGameMultiplayer.Instance.StartHost(); });
        clientBtn.onClick.AddListener(() =>{ KitchenGameMultiplayer.Instance.StartClient(); });
        playAgain.onClick.AddListener(() => { NetworkManager.Singleton.Shutdown(); });


        KitchenGameManager.Instance.OnLocalPlayerReadyChanged += Instance_OnLocalPlayerReadyChanged;
        KitchenGameManager.Instance.OnStateChanged += Instance_OnStateChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        KitchenGameManager.Instance.OnGamePaused += KitchenGameManager_OnGamePaused;
        KitchenGameManager.Instance.OnGameUnpaused += KitchenGameManager_OnGameUnpaused;

        tutorialPanel.SetActive(true);
        tutorialPanel.GetComponent<Button>().onClick.AddListener(() => { KitchenGameManager.Instance.Interact(); });
    }

    #region Screen Buttons
    public void Resume()
    {
        KitchenGameManager.Instance.TogglePauseGame();
    }
    public void MainMenu()
    {
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
    #endregion

    private void Instance_OnLocalPlayerReadyChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsLocalPlayerReady()) waitingPanel.SetActive(true);
    }
    private void Instance_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsCountdownToStartActive()) waitingPanel.SetActive(false);
    }

    private void Singleton_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            hostDisconnectPanel.SetActive(true);
        }
    }
    
    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver())
        {
            gameoverPanel.SetActive(true);
            HighestDishesDeliverdText.text = DeliveryManager.Instance.HighestDishesDelieverd.Value.ToString();
            dishesDeliveredText.text = DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
        }
    }
    private void KitchenGameManager_OnGameUnpaused(object sender, System.EventArgs e)
    {
        pausePanel.SetActive(true);
    }

    private void KitchenGameManager_OnGamePaused(object sender, System.EventArgs e)
    {
        pausePanel.SetActive(false);
    }

}
