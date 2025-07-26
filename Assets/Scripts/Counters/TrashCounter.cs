using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TrashCounter : BaseCounter {


    public static event EventHandler OnAnyObjectTrashed;

    new public static void ResetStaticData() {
        OnAnyObjectTrashed = null;
    }


    public override void Interact(Player player) 
    {
        if (player.HasKitchenObject()) 
        {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());

            InteractServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void InteractServerRpc()
    {
        InteractClientRpc();
    }

    [ClientRpc]
    void InteractClientRpc()
    {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }

}