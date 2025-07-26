using UnityEngine;
using UnityEngine.UI;

public class ColorSelectionUI : MonoBehaviour
{

    public int colorId;
    public Image image;
    public GameObject selected;


    void Start()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChange += Instance_OnPlayerDataListChange;
        image.color = KitchenGameMultiplayer.Instance.GetPlayerColor(colorId);
        IsSelected();
    }

    private void Instance_OnPlayerDataListChange(object sender, System.EventArgs e)
    {
        IsSelected();
    }

    void IsSelected()
    {
        if(KitchenGameMultiplayer.Instance.GetPlayerData().colorId == colorId) selected.SetActive(true);

        else selected.SetActive(false);
    }

    public void ChangeColour()
    {
        KitchenGameMultiplayer.Instance.ChangePlayerColor(colorId);
    }

    private void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChange -= Instance_OnPlayerDataListChange;
    }
}
