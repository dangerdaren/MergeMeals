using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace MergeMeals
{

    public class LevelManager : MonoBehaviour
    {

        public Level currentLevel;
        [SerializeField] private int points = 0;
        [SerializeField] private TMP_Text scoreText;


        [Space]
        [Header("CAMERA SETTINGS")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private GameObject centerPoint;

        private FloorManager floorManager = null;
        private MergeManager mergeManager = null;
        
        [SerializeField] private DifficultyLevel difficulty;

        private float defaultZoom;
        public UnityEvent<Level> InitializeLevelSettings;


        void Awake()
        {
            floorManager = FindObjectOfType<FloorManager>();
            mergeManager = FindObjectOfType<MergeManager>();
            
        }


        void Start()
        {
            BuildScene();
            InitializeLevelSettings.Invoke(currentLevel); // All subscribed Managers will now initialize their settings. See Inspector for Subscriber list.
            difficulty = currentLevel.Difficulty; // TODO Should this be owned and operated by LevelManager rather than Level? Maybe determines bonuses/debuffs to Level's settings (shortens Level's time), etc?
            UpdateScore();
        }


        private void BuildScene()
        {
            floorManager.MakeFloor(currentLevel.BoardSize.x, currentLevel.BoardSize.y);
            mergeManager.InitializeBoardSize(currentLevel.BoardSize.x, currentLevel.BoardSize.y);
            PositionVirtualCamera();
        }


        private void PositionVirtualCamera()
        {
            //Vector3 centerPointPos = new Vector3(level.Width / 2.0f -.5f, 0.0f, level.Height / 2.0f -.5f);
            Vector3 centerPointPos = new Vector3(currentLevel.BoardSize.x / 2.0f - .5f, 0.0f, currentLevel.BoardSize.y / 2.0f - .5f);

            centerPoint.transform.position = centerPointPos;
            print($"Center is: {centerPointPos}");

            defaultZoom = Mathf.Max(currentLevel.BoardSize.x, currentLevel.BoardSize.y);
            float zoomedOutDistance = defaultZoom - (defaultZoom / 3);
            virtualCamera.m_Lens.OrthographicSize = zoomedOutDistance;
        }



        public void UpdateScore()
        {
            scoreText.text = $"{points}";
        }


        public void UpdateScore(int pointMod, bool addPoints)
        {

            if (addPoints) { points += pointMod; }
            else { points -= pointMod; }

            scoreText.text = $"{points}";
        }

    }
}
