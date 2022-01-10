using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeMeals
{

    [CreateAssetMenu(fileName = "FoodItem", menuName = "Merge Meals/SO: Food Item", order = 0)]
    public class FoodItem : ScriptableObject
    {
        [SerializeField] public string itemName;
        [SerializeField] private int pointReward;
        public int PointReward => pointReward;

        [SerializeField] public int mergeLevel;
        [SerializeField] public FoodItem[] createsItem;
        [SerializeField] public FoodItem[] ingredients;
        [SerializeField] public bool isUnlocked;
        [SerializeField] public GameObject itemPrefab;
    }

}
