using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeMeals
{
    //[System.Serializable]
    public class ListableIngredient
    {
        public FoodItem ingredientItem;
        public IngredientAttribute attributes = new IngredientAttribute();

        //[System.Serializable]
        public class IngredientAttribute
        {
            public bool ingredientQueued = false;
            public bool ingredientSpawned = false;
        }
    }
}
