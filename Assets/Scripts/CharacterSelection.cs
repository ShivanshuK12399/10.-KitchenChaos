using System.Collections.Generic;
using UnityEngine;

public class CharacterSelection : MonoBehaviour
{

    public int index;
    public GameObject ready;
    public PlayerVisual playerVisual;

    private void Start()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChange += Instance_OnPlayerDataListChange;
        CharacterSelectionUI.instance.OnReadyChange += Instance_OnReadyChange;
        UpdatePlayer();
    }

    private void Instance_OnReadyChange(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void Instance_OnPlayerDataListChange(object sender, System.EventArgs e)
    {
        UpdatePlayer();   
    }

    void UpdatePlayer()
    {
        if (KitchenGameMultiplayer.Instance.IsPlayerConnected(index))
        {
            gameObject.SetActive(true);

            var playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromIndex(index);
            ready.SetActive(CharacterSelectionUI.instance.IsPlayerReady(playerData.clientId));

            playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
        }
        else gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChange -= Instance_OnPlayerDataListChange;
    }
}
