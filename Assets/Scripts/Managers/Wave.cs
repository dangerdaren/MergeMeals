using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeMeals
{
    [System.Serializable]
    public class Wave
    {
        public int waveID;
        public Order[] order;

        [System.Serializable]
        public class Order
        {
            [Header ("FIXED VALUES")]
            public Vector2Int waveOrderID;
            public int timer;

            [Space]

            [Header ("MODIFIED IN RUNTIME")]
            public OrderStatus orderStatus;
            public bool ranOutOfTime = false;
            public bool madeIncorrectly = false;
            public bool isOrderSucess = false;
            // enum or bools for various fail conditions?

            public FoodItem[] orderItem;
            public List<ListableIngredient> listableIngredients = new List<ListableIngredient>();
        }
    }

    public enum OrderStatus
    {
        WaitingForQueue,
        FirstTry,
        SecondTry,
        ThirdTry,
        OrderFailed,
        OrderSuccess,
        Error
    }

}
