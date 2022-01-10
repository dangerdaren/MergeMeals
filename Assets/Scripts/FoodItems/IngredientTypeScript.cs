using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MergeMeals
{
    public class IngredientTypeScript : MonoBehaviour
    {
        public FoodItem foodItem;
        [Header("SPAWN INFO")]
        public Vector2Int waveOrderID;

        [Header("For combo feature down the road:")]
        public int comboBonusAmount; //if not 0, can merge with x combos.

        [SerializeField] private GameObject actualGraphic;
        [SerializeField] private TMP_Text placeHolderText;
        [SerializeField] private TMP_Text orderIDText;
        //public bool usePlaceholderText = true;    //Changed to a enum, I (Will) needed a third type for a dud

        [Header("DISPLAY TYPE")]
        public DisplayType displayType = DisplayType.graphic;
        public enum DisplayType
        {
            text,
            graphic,
            none
        };


        private void Awake()
        {

        }


        private void Start()
        {
            SetPlaceholderText(); // TODO Once all the graphics are in place, we can remove all this stuff.
        }


        private void SetPlaceholderText()
        {
            
            switch (displayType)
            {
                case DisplayType.text:
                {
                    actualGraphic.SetActive(false);
                    placeHolderText.enabled = true;
                    placeHolderText.text = foodItem.itemName;
                    orderIDText.enabled = true;
                    orderIDText.text = ($"{waveOrderID.x},{waveOrderID.y}");
                    break;
                }
                case DisplayType.graphic:
                {
                    actualGraphic.SetActive(true);
                    placeHolderText.enabled = false;
                    orderIDText.enabled = true;
                    orderIDText.text = ($"{waveOrderID.x},{waveOrderID.y}");
                    break;
                }
                case DisplayType.none:
                {
                    actualGraphic.SetActive(false);
                    placeHolderText.enabled = false;
                    orderIDText.enabled = false;
                    orderIDText.text = ""; // TODO Can't seem to get the DUD or Placeholder to stop showing the orderID... ask Will.
                    break;
                }
            }
        }


        public void SetDisplayType(string type) //Called from the MergeManager
        {
            switch(type)
            {
                case "text":
                    displayType = DisplayType.text;
                    break;
                case "graphic":
                    displayType = DisplayType.graphic;
                    break;
                case "none":
                    displayType = DisplayType.none;
                    break;
                default:
                    displayType = DisplayType.graphic;
                    break;
            }
        }
    }
}
