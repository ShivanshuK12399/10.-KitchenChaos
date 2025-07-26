using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour {


    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;

    public static DeliveryManager Instance { get; private set; }
    public NetworkVariable<int> HighestDishesDelieverd= new NetworkVariable<int>();
    public RecipeListSO recipeListSO;


    List<RecipeSO> waitingRecipeSOList;
    float spawnRecipeTimer = 4f;
    float spawnRecipeTimerMax = 4f;
    int waitingRecipesMax = 4;
    int dishedDelivered;


    void Awake() 
    {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
    }

    void Update() 
    {
        if (IsServer)
        {
            spawnRecipeTimer -= Time.deltaTime;
            if (spawnRecipeTimer <= 0f)
            {
                spawnRecipeTimer = spawnRecipeTimerMax;

                if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
                {
                    int index = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                    
                    SpawnWaitingRecipeClientRpc(index);
                }
            }
        }
    }

    [ClientRpc]
    void SpawnWaitingRecipeClientRpc(int index)
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[index];
        waitingRecipeSOList.Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }


    public void DeliverRecipe(PlateKitchenObject plateKitchenObject) 
    {
        for (int i = 0; i < waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) 
            {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) 
                {
                    // Cycling through all ingredients in the Recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) 
                    {
                        // Cycling through all ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound) {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!

                    // client -> server -> all client, in this way all client will know
                    // that recipe is delivered 
                    DeliveredCorrectRecipeServerRpc(i); // i is correct recipe index
                    print(dishedDelivered++);
                    return;
                }
            }
        }

        // No matches found!
        // Player did not deliver a correct recipe
        DeliveredIncorrectRecipeServerRpc(); // if we dont run this on server then other clients will not get
                                            // notified that recipe is delivered
    }

    [ServerRpc(RequireOwnership =false)]
    void DeliveredCorrectRecipeServerRpc(int i)
    {
        DeliveredCorrectRecipeClientRpc(i);
    }

    [ClientRpc]
    void DeliveredCorrectRecipeClientRpc(int i)
    {
        HighestDishesDelieverd.Value++;

        waitingRecipeSOList.RemoveAt(i);

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership =false)]
    void DeliveredIncorrectRecipeServerRpc()
    {
        DeliveredIncorrectRecipeClientRpc();
    }

    [ClientRpc]
    void DeliveredIncorrectRecipeClientRpc()
    {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount() {
        return dishedDelivered;
    }

}
