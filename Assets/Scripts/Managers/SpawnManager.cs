using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MergeMeals
{
    [System.Serializable]
    public class SpawnManager : MonoBehaviour
    {

        [Header("GAMEOBJECTS")]
        [SerializeField] private GameObject mergeManagerObj;
        private MergeManager mergeManager;

        [Header("CURRENT")]
        [SerializeField] private DifficultyLevel difficulty;
        [Space]
        [SerializeField] private int spawnAmount = 3;

        [Space]
        [SerializeField] private Level currentLevel;
        [SerializeField] private Vector2Int boardSize;

        
        [Header("LINE UP")]
        //[SerializeField] private Vector2Int previousWaveOrder;
        [SerializeField] public Vector2Int queuedWaveOrder;
        [SerializeField] private Vector2Int nextWaveOrder;
        [SerializeField] private float waitTime = 1f;       //I created this because I was testing the match pre-check

        [SerializeField] private Queue<FoodItem> spawnQueue = new Queue<FoodItem>();
        [SerializeField] private Queue<Vector2Int> spawnQueueOrderReference = new Queue<Vector2Int>();


        private void Awake()
        {
            
        }


        public void InitializeLevelSettings(Level level)
        {
            currentLevel = level;

            difficulty = currentLevel.Difficulty;
            boardSize = currentLevel.BoardSize;
            mergeManager = mergeManagerObj.GetComponent<MergeManager>();
        }


        //public void SetWaveOrder(Vector2Int currentWaveOrder, Vector2Int nextWaveOrder) //TODO remove this?
        //{
        //    this.currentWaveOrder = currentWaveOrder;
        //    this.nextWaveOrder = nextWaveOrder;

        //}

        public void QueueItemsToSpawn(Wave.Order order, List<ListableIngredient> ingredients)
        {
            queuedWaveOrder = order.waveOrderID;
            foreach (ListableIngredient ingredient in ingredients)
            {
                //Debug.Log($"{ingredient.ingredientItem.name}'s default attributes are set to: {ingredient.attributes.ingredientQueued} and {ingredient.attributes.ingredientSpawned}");
                //Debug.Log($"Queuing {ingredient.ingredientItem.name}");
                

                if (ingredient.attributes.ingredientQueued == false)
                {
                    spawnQueue.Enqueue(ingredient.ingredientItem); // Adds the FoodItems to a spawn queue
                    //spawnQueueOrderReference.Enqueue(order); // Since above queue cannot contain order id, this is a workaround. // TODO storing in ingredient may fix this.

                    ingredient.attributes.ingredientQueued = true;
                    //Debug.Log($"{ingredient.ingredientItem.name}'s default attributes are set to: {ingredient.attributes.ingredientQueued} and {ingredient.attributes.ingredientSpawned}");

                }
            }
            //Debug.Log($"SpawnQueue has {spawnQueue.Count()} items ready to spawn!");
        }


        public IEnumerator CheckLocation()
        {
            if (spawnQueue.Count !=0 && mergeManager != null)
            {
                bool recheck = true;
                while (spawnQueue.Count > 0)    //Continues to recheck spawnQueue until it's actually emptied. Allows for a "wait" when the board is fully occupied.
                {
                    FoodItem foodItem = spawnQueue.Dequeue();
                    while (recheck)
                    {
                        recheck = false;
                        CoroutineWithData cd = new CoroutineWithData(this, mergeManager.GetVacantAndNonMergingLocation(foodItem));
                        yield return cd.coroutine;      // Waiting for Function GetVacantAndNonMergingLocation()
                        if (cd.result is Vector3Int)    //Since the CoroutineWithData() can return any class type, this confirms its a Vector3Int
                        {
                            Vector3Int spawnLocation = (Vector3Int)cd.result;
                            if (Vector3.Distance(spawnLocation,Vector3Int.one*(-999)) <= 0.01f) // error checking
                            {
                                Debug.LogError("THERE ARE NO VACANT LOCATIONS THAT WILL NOT CAUSE A MERGE. F*** SpawnManager! -Signed, MergeManager.");
                            }
                            else
                            {
                                SpawnIngredients(foodItem, spawnLocation);
                            }
                        }
                        else
                        {
                            recheck = true;
                            spawnQueue.Enqueue(foodItem); // Is this a problem for Reference Queue?
                        }
                        yield return new WaitForSeconds(waitTime);
                    }
                    recheck = true; //Resets "recheck" for the next foodItem
                }
            }
        }

        private Vector2Int SpawnIngredients(FoodItem foodItem, Vector3Int spawnLocation)
        {
            GameObject ingredientPrefab = Instantiate(foodItem.itemPrefab, spawnLocation, Quaternion.identity, transform);
            //orderID = spawnQueueOrderReference.Dequeue(); // is this getting dequeued more than once?
            //Debug.Log($"Spawning {ingredientPrefab.GetComponent<IngredientTypeScript>().foodItem.itemName} at {spawnLocation}");

            ingredientPrefab.GetComponent<IngredientTypeScript>().waveOrderID = queuedWaveOrder; // assign the ingredient's wave.order for future reference.
            foreach (ListableIngredient ingredient in currentLevel.Wave[queuedWaveOrder.x].order[queuedWaveOrder.y].listableIngredients)
            {
                ingredient.attributes.ingredientSpawned = true;
                ingredient.attributes.ingredientQueued = false;
            }

            return queuedWaveOrder;
        }
    }
}
