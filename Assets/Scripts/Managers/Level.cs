using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace MergeMeals
{   
    [CreateAssetMenu(fileName = "Level", menuName = "Merge Meals/New Level", order = 0)]
    public class Level : ScriptableObject
    {
        [Header("BOARD SIZE")]
        [SerializeField] private Vector2Int boardSize;
        public Vector2Int BoardSize => boardSize;

        [Space]
        [Header("LEVEL SETTINGS")]
        [SerializeField] private DifficultyLevel difficulty; // Currently set up only to adjust timer and ability to check recipe ingredients.
        public DifficultyLevel Difficulty => difficulty;

        [SerializeField] private Wave[] wave;
        public Wave[] Wave => wave;
    }

    public enum DifficultyLevel // Easy, can fail once due to either time OR wrong. Med can fail once due to wrong. All others, order is canceled.
    {
        Easy,
        Medium,
        Hard
    }
}
