using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MergeMeals
{

    public class UIOrderUp : MonoBehaviour
    {
        [SerializeField] private TMP_Text orderText;
        [SerializeField] private TMP_Text recipeText;
        [SerializeField] private FoodItem[] order;
        public FoodItem[] Order => order;
        [SerializeField] private List<ListableIngredient> recipe;
        

        public float timer;
        public bool beginTimer = false;

        public Wave.Order thisWaveOrder = null;

        //debug
        [SerializeField] private TMP_Text waveOrderText;
        [SerializeField] private TMP_Text statusText;
        //debug

        public Vector2Int orderID; // simply the WaveCustomerOrder from SpawnManager

        void Awake()
        {
            
        }

        void Start()
        {
            
        }


        public void InitializeFields(Level level, Vector2Int providedOrderID) //TODO For some reason retry orders don't include recipe ingredients...
        {
            orderID = providedOrderID;
            thisWaveOrder = level.Wave[providedOrderID.x].order[providedOrderID.y];
            order = thisWaveOrder.orderItem;
            recipe = thisWaveOrder.listableIngredients;
            gameObject.name = ($"{providedOrderID.x}, { providedOrderID.y}");

            //debug
            waveOrderText.text = providedOrderID.ToString();
            statusText.text = thisWaveOrder.orderStatus.ToString();
            //debug

            timer = thisWaveOrder.timer;
            beginTimer = true;

            orderText.text = null;

            for (int i = 0; i < order.Length; i++)
            {
                orderText.text += order[i].itemName + "\n";
                
            }

            if (level.Difficulty == DifficultyLevel.Easy)
            {
                recipeText.text = null;
                
                foreach (ListableIngredient ingredient in recipe)
                {
                    recipeText.text += " \u2022 " + ingredient.ingredientItem.itemName + "\n";
                }
                Debug.Log($"Initialized order {thisWaveOrder.orderItem} with recipe: {recipeText.text}"); // TODO Doesn't seem to list all ingredients?
            }
            else if ( level.Difficulty != DifficultyLevel.Easy)
            {
                recipeText.text = null;
            }


        }

        public void ClearFields() // Called if order is completed successfully and the OrderUpUI needs to get recycled.
        {
            order = null;
            orderID = new Vector2Int(-1,-1);
            timer = 0;
            beginTimer = false;
            
            orderText.text = "";
            recipeText.text = "";
            statusText.text = "";
        }
    }

}
