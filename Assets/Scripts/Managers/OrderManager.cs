using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using TMPro;

namespace MergeMeals
{

    public class OrderManager : MonoBehaviour
    {
        [SerializeField] private Level currentLevel;
        [SerializeField] private DifficultyLevel difficulty;
        [SerializeField] private int secondsBetweenEachOrder = 10;
        [SerializeField] private bool autoQueueOrders = true;
        [SerializeField] private TMP_InputField waveInput;
        [SerializeField] private TMP_InputField orderInput;

        [Header("LINEUP")]
        [SerializeField] TMP_Text orderTagsShown;
        [SerializeField] GameObject ordersPanel;
        [SerializeField] private GameObject[] orderTags;

        [SerializeField] int currentOrdersUpAmount = 0;
        [Space]
        [SerializeField] private Vector2Int currentUntriedOrderID = new Vector2Int(0, -1);
        [SerializeField] private Vector2Int nextUntriedOrderID = new Vector2Int(0, 0);
        private Queue<Vector2Int> retryOrderIDs = new Queue<Vector2Int>();
        [Space]
        [SerializeField] private OrderStatus mostRecentOrderStatus;
        public List<Vector2Int> openOrders = new List<Vector2Int>();
        public List<Vector2Int> completedOrders = new List<Vector2Int>();
        //[SerializeField] UnityEvent<Vector2Int> newOrderUp;
        //[SerializeField] private bool noCurrentOrders = true;
        

        //[SerializeField] private bool shouldQueueUpOrder = true;


        private SpawnManager spawnManager = null;
        private LevelManager levelManager = null;


        void Awake()
        {
            mostRecentOrderStatus = OrderStatus.WaitingForQueue;
            spawnManager = FindObjectOfType<SpawnManager>();
            levelManager = FindObjectOfType<LevelManager>();
            completedOrders.Clear();


            foreach (GameObject orderTag in orderTags)
            {
                orderTag.SetActive(false);
            }

        }


        private void Start()
        {
            StartCoroutine(StandardOrderQueueUp());
        }


        void Update()
        {
            TestOnPress();
            orderTagsShown.text = ($"Orders Up: {currentOrdersUpAmount}");
        }


        public void InitializeLevelSettings(Level level) // Invoked via Level Manager's Unity Event
        {
            currentLevel = level;
            difficulty = currentLevel.Difficulty;
            

            BuildIngredientList(); // Also autopopulates Wave Order IDs in the Level SO for error checking purposes.
            currentUntriedOrderID = new Vector2Int (0, -1);
            nextUntriedOrderID = IncrementWaveOrderID(currentUntriedOrderID);
            
        }

        // For testing purposes only.
        public void TestOnPress()
        {
            if (Input.GetKeyDown(KeyCode.Q)) // Press Q to Queue up the current order's ingredient list.
            {
                PushNextOrder(); //TODO SEE IF THIS IS STILL TRUE! : If Q pushes an order off the screen, it won't respawn the ingredients for some reason. Will if naturally pushed. Also, this won't push failed orders?
                
            }

            if (Input.GetKeyDown(KeyCode.S)) // Press S to succeed an order.
            {
                OrderSuccess(ParseWaveOrderInput());

            }

            if (Input.GetKeyDown(KeyCode.T)) // Press T to fail the order due to time. (orderID determined by input fields on screen).
            {
                OrderTookTooLong(ParseWaveOrderInput());
                
            }

            if (Input.GetKeyDown(KeyCode.I)) // Press I to fail the order due to incorrectness. (orderID determined by input fields on screen).
            {
                OrderMadeIncorrectly(ParseWaveOrderInput());

            }

        }

        // Also for testing purposes (feeds input WaveOrder data to TestOnPress).
        public Vector2Int ParseWaveOrderInput()
        {
            Vector2Int orderIDtoTest = new Vector2Int(int.Parse(waveInput.text), int.Parse(orderInput.text));
            return orderIDtoTest;
        }

        // Standard gameloop. Pulls from prebuild LevelSO orders and begins pushing them through.
        public IEnumerator StandardOrderQueueUp() // If Auto Queue Orders bool is enabled, orders will queue themselves every x seconds (based on difficult).
        {
            while (autoQueueOrders)
            {
                yield return new WaitForSeconds(secondsBetweenEachOrder);
                PushNextOrder();

            }
        }

        // Checks for any retryable orders to slip into the queue, elsewise continues with LevelSO's list.
        private void PushNextOrder()
        {
            if (retryOrderIDs.Count == 0)
            {
                MoveToNextUntriedOrder();
            }
            else
            {
                QueueUpIngredientsFromOrder(retryOrderIDs.Dequeue());
            }
        }

        // Increments through list of untried orders and beings queueing ingredients.
        public void MoveToNextUntriedOrder()
        {
            IncrementUntriedOrderID();
            QueueUpIngredientsFromOrder(currentUntriedOrderID);
        }

        // Queues up ingredients for all applicable orders and tells SpawnManager to deal with them.
        public void QueueUpIngredientsFromOrder(Vector2Int providedOrderID) //TODO Eventually needs a check to make sure waveOrder is valid.
        {
            int thisWave = providedOrderID.x;
            int thisOrder = providedOrderID.y;

            Wave.Order orderToQueue = currentLevel.Wave[thisWave].order[thisOrder];

            if (orderToQueue.ranOutOfTime && orderToQueue.orderStatus != OrderStatus.OrderFailed)
            {
                OrderUp(providedOrderID); // Bring back the order tag, but don't respawn ingredients, since they should still be on the board.
            }
            else if (orderToQueue.orderStatus != OrderStatus.OrderFailed ||
                     orderToQueue.orderStatus != OrderStatus.OrderSuccess ) // If the status is not Failed or Success, queue up order.
            {
                if (orderToQueue.orderStatus == OrderStatus.WaitingForQueue)
                {
                    mostRecentOrderStatus = orderToQueue.orderStatus = GetSetOrderStatus(orderToQueue, true);
                }

                //Debug.Log($"Most Recent Order's Status: {mostRecentOrderStatus}");
                //Debug.Log($"{orderToQueue.ToString()}'s status is: {orderToQueue.orderStatus}");

                //Debug.Log($"OrderManager says Ingredient List has: {orderToQueue.listableIngredients.Count} items");

                List<ListableIngredient> ingredientsToQueue = new List<ListableIngredient>(orderToQueue.listableIngredients);

                spawnManager.QueueItemsToSpawn(orderToQueue, ingredientsToQueue);
                OrderUp(providedOrderID);
                BeginSpawnProcess();

            }
            else
            {
                Debug.LogError($"CANNOT QUEUE {providedOrderID}, AS ITS STATUS IS: {mostRecentOrderStatus}, and/or it's ingredients are still available!");
            }

        }

        // Tells SpawnManager to do its thing.
        private void BeginSpawnProcess()
        {
            StartCoroutine(spawnManager.CheckLocation());
        }

        // Increments a given WaveOrder based on provided ID and checks against LevelSO information.
        private Vector2Int IncrementWaveOrderID(Vector2Int providedOrderID)
        {
            int thisWave = providedOrderID.x;
            int thisOrder = providedOrderID.y;

            int totalOrdersInThisWave = currentLevel.Wave[thisWave].order.Length;
            int totalWavesInLevel = currentLevel.Wave.Length;

            if (thisOrder < totalOrdersInThisWave - 1)
            {
                thisOrder++;
            }
            else
            {
                thisOrder = 0;

                if (thisWave < totalWavesInLevel - 1)
                {
                    thisWave++;
                }
                else // setting this to 99 throws the IndexOutOfRangeException. Just means there are no more waves.
                {
                    thisWave = 99;
                    thisOrder = 99;
                }
            }
            return new Vector2Int(thisWave, thisOrder);
        }

        // Calls the generic WaveOrder incrementor on the untried order list.
        private void IncrementUntriedOrderID()
        {
            currentUntriedOrderID = nextUntriedOrderID;
            nextUntriedOrderID = IncrementWaveOrderID(currentUntriedOrderID);
        }

        // Deals with the orders on deck and what to do with them as they pile up. Tied to UI.
        public void OrderUp(Vector2Int providedOrderID)
        {
            if (currentOrdersUpAmount == 6) // Once the order gets to the end of the line, it is too late. Order fails. GameObject drops to the bottom for new order.
            {
                GameObject resetThisOrder = ordersPanel.transform.GetChild(0).gameObject;
                Vector2Int orderToFail = resetThisOrder.GetComponent<UIOrderUp>().orderID; // TODO DOES THIS STILL NEED WORK? Wasn't working correctly when pressing Q--didn't seem to respawn items. Check again!

                OrderTookTooLong(orderToFail);

            }

            if (currentOrdersUpAmount < 6) // If there is room for an order, bring it in!
            {
                AddNewOrderUp(providedOrderID);
            }

        }

        // Sorts through pool of Order UI elements and brings the proper one in at the proper location.
        private void AddNewOrderUp(Vector2Int providedOrderID)
        {
            currentOrdersUpAmount++;
            openOrders.Add(providedOrderID);

            foreach (GameObject order in orderTags) // Breaking out of the loop enables only one order, not all of them :)
            {
                if (order.activeInHierarchy == false)
                {
                    order.transform.SetAsLastSibling();
                    order.SetActive(true);
                    order.GetComponent<UIOrderUp>().InitializeFields(currentLevel, providedOrderID);
                    //StartCoroutine(CountDown(order.GetComponent<UIOrderUp>())); // TODO Enable this once method is properly set up.
                    break;
                }
            }
        }

        // Returns the needed order info of the provided OrderID based on UI elements.
        public UIOrderUp GetActiveOrderTag(Vector2Int providedOrderID)
        {
            UIOrderUp correctOrderTag = null;

            foreach (GameObject order in orderTags)
            {
                if (order.activeInHierarchy == true)
                {
                    if (order.GetComponent<UIOrderUp>().orderID == providedOrderID)
                    {
                        correctOrderTag = order.GetComponent<UIOrderUp>();
                    }
                }
            }
            return correctOrderTag;
        }

        // Efficiency tool for returning the WaveOrder for a provided OrderID.
        public Wave.Order GetWaveOrder (Vector2Int providedOrderID)
        {
            int thisWave = providedOrderID.x;
            int thisOrder = providedOrderID.y;
            Wave.Order thisWaveOrder = currentLevel.Wave[thisWave].order[thisOrder];

            return thisWaveOrder;

        }

        // Efficiency tool for returning the Point Value of an order for adding or subtracting to the score.
        public int GetOrderPointValue (Wave.Order providedWaveOrder)
        {
            List<int> individualOrderPoints = new List<int>();
            int totalPointReward = 0;

            for (int i = 0; i < providedWaveOrder.orderItem.Length; i++) 
            {
                individualOrderPoints.Add(providedWaveOrder.orderItem[i].mergeLevel); // Remove pointReward and just use mergeLevel as point reward?
            }

            foreach (int point in individualOrderPoints) // TODO this is just a test, ultimately will reward player for each successful part of multi-part order? Or all or nothing?
            {
                totalPointReward += point;
            }

            return totalPointReward;

        }

        // For when an order took too long to make.
        public void OrderTookTooLong(Vector2Int providedOrderID)
        {
            Wave.Order thisWaveOrder = GetWaveOrder(providedOrderID);
            UIOrderUp thisOrderTag = GetActiveOrderTag(providedOrderID);
            
            thisWaveOrder.ranOutOfTime = true;
            mostRecentOrderStatus = thisWaveOrder.orderStatus = GetSetOrderStatus(thisWaveOrder, true);
            Debug.Log($"ORDER FAILED! You took too long on: {providedOrderID}! Its status is now {thisWaveOrder.orderStatus}");

            int pointMod = GetOrderPointValue(thisWaveOrder);
            levelManager.UpdateScore(pointMod, false);

            AddOrderToRetryQueueIfApplicable(providedOrderID);

            ResetOrder(thisOrderTag.gameObject);
        }

        // For when an order was made incorrectly.
        public void OrderMadeIncorrectly(Vector2Int providedOrderID)
        {
            Wave.Order thisWaveOrder = GetWaveOrder(providedOrderID);
            UIOrderUp thisOrderTag = GetActiveOrderTag(providedOrderID);

            thisWaveOrder.madeIncorrectly = true;
            mostRecentOrderStatus = thisWaveOrder.orderStatus = GetSetOrderStatus(thisWaveOrder, true);
            Debug.Log($"ORDER FAILED! You incorrectly prepared order: {providedOrderID}! Its status is now {thisWaveOrder.orderStatus}");

            int pointMod = GetOrderPointValue(thisWaveOrder);
            levelManager.UpdateScore(pointMod, false);

            AddOrderToRetryQueueIfApplicable(providedOrderID);

            ResetOrder(thisOrderTag.gameObject);
        }

        // If an order took too long or made incorrectly and is able to be retried.
        private void AddOrderToRetryQueueIfApplicable(Vector2Int providedOrderID)
        {
            Wave.Order orderToCheck = GetWaveOrder(providedOrderID);
            OrderStatus statusToCheck = GetSetOrderStatus(orderToCheck, false);

            if (statusToCheck == OrderStatus.OrderFailed)
            {
                Debug.LogError($"FAILURE! {providedOrderID}'s status is {statusToCheck}"); // TODO these debugs gave different results than expect and helped solve the issue. Switch back and see if that matters.
                return;
            } 
            
            else if (statusToCheck != OrderStatus.OrderFailed &&
                     statusToCheck != OrderStatus.Error)
            {
                Debug.LogError($"{providedOrderID}'s status is {statusToCheck}");
                retryOrderIDs.Enqueue(providedOrderID);
            }
        }

        // MergeManager delivers the WaveOrders of the succesful merge. This determines which Order should be marked complete based on lowest OrderID.
        // TODO Will need some work once Will creates feature.
        public void DetermineSuccessfulOrder(GameObject providedCreatedItem, GameObject providedOrderTag, List<Vector2Int> providedOrderIDList)
        {
            FoodItem createdItem = providedCreatedItem.GetComponent<IngredientTypeScript>().foodItem;

            UIOrderUp openOrderTagInfo = providedOrderTag.GetComponent<UIOrderUp>();
            FoodItem[] recipes = openOrderTagInfo.Order;
            Vector2Int tagOrderID = openOrderTagInfo.orderID;

            
            if (recipes.Contains(createdItem) && providedOrderIDList.Contains(tagOrderID))
            {
                OrderSuccess(tagOrderID);
            }
            else
            {
                // ERROR!
                QueueUpIngredientsFromOrder(tagOrderID); // Respawn ingredients if eligible?

            }


            //Vector2Int orderToComplete = new Vector2Int();

            //foreach (Vector2Int orderID in providedOrderIDList)
            //{
            //    if (openOrders.Contains(orderID))
            //    {
            //        Debug.Log($"Checking {orderID.ToString()}");
            //        if (completedOrders.Contains(orderID)) { Debug.Log($"{orderID.x}.{orderID.y} has already been completed. Moving on..."); continue; }

            //        if (!completedOrders.Contains(orderID))
            //        {
            //            Debug.Log($"{orderID.x}.{orderID.y} IS NOW COMPLETE!");
            //            completedOrders.Add(orderID);
            //            orderToComplete = orderID;
                        
            //            break;
            //        }
            //    }
            //}


            //providedOrderIDList = providedOrderIDList.OrderBy(x => x.x).ThenBy(y => y.y).ToList();

            //List<FoodItem> sharedRecipes = new List<FoodItem>();

            // First determine the recipe all these ingredients have most in common.
            //      Then check for the lowest common OrderID.


            //List<FoodItem> mostSharedRecipe = new List<FoodItem>();


            //foreach (GameObject ingredientInstance in ingredientList)
            //{
            //    foreach (FoodItem recipe in ingredientInstance.GetComponent<FoodItem>().createsItem)
            //    {
            //        ingredientRecipeList.Add(recipe);
            //    }
            //}


            //foreach (FoodItem ingredient)



        }

        // Based on the OrderID that has been determined to the completed, this will mark that order complete and remove the open order tag.
        private void OrderSuccess(Vector2Int providedOrderID)
        {
            Wave.Order thisWaveOrder = GetWaveOrder(providedOrderID);
            UIOrderUp thisOrderTag = GetActiveOrderTag(providedOrderID);
            
            thisWaveOrder.isOrderSucess = true;
            mostRecentOrderStatus = thisWaveOrder.orderStatus = GetSetOrderStatus(thisWaveOrder, true);
            Debug.Log($"ORDER SUCCEEDED! You correctly prepared order: {providedOrderID}! Its status is now {thisWaveOrder.orderStatus}");

            int pointMod = GetOrderPointValue(thisWaveOrder);
            levelManager.UpdateScore(pointMod, true);

            ResetOrder(thisOrderTag.gameObject);
        }
        
        // Tied to UI. Resets the tag whenever needed.
        private void ResetOrder(GameObject orderTag)
        {
            UIOrderUp orderInfo = orderTag.GetComponent<UIOrderUp>();

            openOrders.Remove(orderInfo.orderID);
            orderInfo.ClearFields();
            orderTag.transform.SetAsLastSibling();
            orderTag.SetActive(false);
            currentOrdersUpAmount--;
        }

        // Each order has a preset countdown after appearing. At the end, it is marked TookTooLong.
        // TODO currently in progress so the method call is currently commented out.
        // TODO set this number based on Difficulty on Awake, not arbitrary assignment.
        // TODO this may not be working correctly. Some timers seemed to be moving too fast.
        public IEnumerator CountDown(UIOrderUp order) // Is this good practice: Getting and Setting variables in another class to avoid Circular dependency?
        {
            Vector2Int orderID = order.orderID;
            
            while (order.beginTimer && order.timer > 0)
            {
                yield return new WaitForSeconds(1f);
                order.timer--;
                Debug.Log($"Time left: {order.timer}");
            }
            if (order.timer >= 0)
            {
                OrderTookTooLong(orderID);
            }

        }


        // Returns the current order's status, or sets it to the next proper state if bool argument is true.
        public OrderStatus GetSetOrderStatus(Wave.Order providedWaveOrder, bool incrementOrderStatus)
        {
            bool orderMayFail = !providedWaveOrder.isOrderSucess;

            OrderStatus OrdersCurrentStatus = providedWaveOrder.orderStatus;

            if (OrdersCurrentStatus == OrderStatus.WaitingForQueue)
            {
                if (incrementOrderStatus) { return providedWaveOrder.orderStatus = OrderStatus.FirstTry; }

                return OrdersCurrentStatus;

            }
            else if (OrdersCurrentStatus == OrderStatus.FirstTry)
            {
                if (difficulty == DifficultyLevel.Hard)
                {
                    if (incrementOrderStatus)
                    {
                        if (orderMayFail) { return providedWaveOrder.orderStatus = OrderStatus.OrderFailed; }
                        else { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                    }

                    return OrdersCurrentStatus;

                }
                else if (difficulty == DifficultyLevel.Medium)
                {
                    if (providedWaveOrder.madeIncorrectly && !providedWaveOrder.ranOutOfTime)
                    {
                        if (incrementOrderStatus) { return providedWaveOrder.orderStatus = OrderStatus.SecondTry; }

                        return OrdersCurrentStatus;

                    }
                    else
                    {
                        if (incrementOrderStatus)
                        {
                            if (orderMayFail) { return providedWaveOrder.orderStatus = OrderStatus.OrderFailed; }
                            else { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                        }

                        return OrdersCurrentStatus;

                    }
                }
                else if (difficulty == DifficultyLevel.Easy)
                {
                    if (incrementOrderStatus)
                    {
                        if (providedWaveOrder.isOrderSucess) { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                        else { return providedWaveOrder.orderStatus = OrderStatus.SecondTry; }
                    }

                    return OrdersCurrentStatus;

                }
            }
            else if (OrdersCurrentStatus == OrderStatus.SecondTry)
            {
                if (difficulty == DifficultyLevel.Medium)
                {
                    if (incrementOrderStatus)
                    {
                        if (orderMayFail) { return providedWaveOrder.orderStatus = OrderStatus.OrderFailed; }
                        else { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                    }

                    return OrdersCurrentStatus;

                }
                if (difficulty == DifficultyLevel.Easy)
                {
                    if ((providedWaveOrder.madeIncorrectly && !providedWaveOrder.ranOutOfTime) || (providedWaveOrder.ranOutOfTime && !providedWaveOrder.madeIncorrectly))
                    {
                        if (providedWaveOrder.isOrderSucess) { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                        if (incrementOrderStatus) { return providedWaveOrder.orderStatus = OrderStatus.ThirdTry; }

                        return OrdersCurrentStatus;

                    }
                }
            }
            else if (OrdersCurrentStatus == OrderStatus.ThirdTry)
            {
                if (incrementOrderStatus)
                {
                    if (providedWaveOrder.isOrderSucess) { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                    if (orderMayFail) { return providedWaveOrder.orderStatus = OrderStatus.OrderFailed; }
                    else { return providedWaveOrder.orderStatus = OrderStatus.OrderSuccess; }
                }

                return OrdersCurrentStatus;

            }
            else if (OrdersCurrentStatus == OrderStatus.OrderFailed)
            {
                if (incrementOrderStatus) { return providedWaveOrder.orderStatus = OrderStatus.Error; }
                else { return OrdersCurrentStatus; }
            }
            return OrderStatus.Error;

        }


        private bool BuildIngredientList() // I'm hideous. Don't look at me!
        {
            // Oh no! Water moccasins (nested for loops)!

            for (int iWave = 0; iWave < currentLevel.Wave.Length; iWave++) // For every Wave
            {
                Debug.Log($"Wave number: {iWave}"); // Announce the wave.
                currentLevel.Wave[iWave].waveID = iWave;

                for (int iOrder = 0; iOrder < currentLevel.Wave[iWave].order.Length; iOrder++) // and every Order
                {
                    currentLevel.Wave[iWave].order[iOrder].waveOrderID = new Vector2Int(iWave, iOrder);
                    //Debug.Log($"Order number: {iWave}.{iOrder}"); //Announce the order

                    // BUILD A QUEUE FOR INGREDIENTS FOR USE LATER
                    Queue<FoodItem> tempFoodQueue = new Queue<FoodItem>();

                    for (int iItem = 0; iItem < currentLevel.Wave[iWave].order[iOrder].orderItem.Length; iItem++) // and every Item of Tier 3 or Tier 2
                    {
                        FoodItem currentItem = currentLevel.Wave[iWave].order[iOrder].orderItem[iItem];

                        //Debug.Log($"{iWave}.{iOrder}.{iItem} Item name: {currentItem.itemName}"); // Announce the Wave.Order.Item sequence and Item name


                        if (currentItem.mergeLevel == 3) // IF TIER 3 ITEM!
                        {
                            //Debug.Log($"{currentItem.itemName} is a Tier 3 item!"); // Announce the item is Tier 3


                            for (int iItemIngredients = 0; iItemIngredients < currentItem.ingredients.Length; iItemIngredients++)
                            {
                                FoodItem tier2Item = currentItem.ingredients[iItemIngredients];


                                if (tier2Item.mergeLevel == 2) // IF TIER 2 ITEM!
                                {
                                    //Debug.Log($"{tier2Item.itemName} is a Tier 2 item!"); // Announce the item is Tier 2


                                    for (int iItemSubIngredient = 0; iItemSubIngredient < tier2Item.ingredients.Length; iItemSubIngredient++)
                                    {
                                        FoodItem actualIngredient = tier2Item.ingredients[iItemSubIngredient];
                                        //Debug.Log($"{actualIngredient.itemName} is a Tier 1 item!");

                                        currentLevel.Wave[iWave].order[iOrder].listableIngredients.Add(new ListableIngredient()); // Add a new unassigned ingredient to the slot
                                        currentLevel.Wave[iWave].order[iOrder].listableIngredients[iItemSubIngredient].attributes = new ListableIngredient.IngredientAttribute(); // add the attribute class to said blank item.
                                        currentLevel.Wave[iWave].order[iOrder].listableIngredients[iItemSubIngredient].ingredientItem = actualIngredient; // and assign the ingredient.
                                        //Debug.Log($"{level.Wave[iWave].order[iOrder].listableIngredients[iItemSubIngredient].ingredientItem.itemName}'s attributes are: {level.Wave[iWave].order[iOrder].listableIngredients[iItemSubIngredient].attributes.ingredientQueued} and {level.Wave[iWave].order[iOrder].listableIngredients[iItemSubIngredient].attributes.ingredientSpawned}");  



                                        //Debug.Log($"Added {actualIngredient.itemName}");

                                        tempFoodQueue.Enqueue(actualIngredient); // add items to the queue from each sub item
                                    }
                                    //Debug.Log($"Added {level.Wave[iWave].order[iOrder].listableIngredients.Count}");


                                }
                            }
                        }


                        if (currentItem.mergeLevel == 2) // IF TIER 2 ITEM!
                        {
                            //Debug.Log($"{currentItem.itemName} is a Tier 2 item and has {currentItem.ingredients.Length} ingredients!"); // Announce the item is Tier 2


                            for (int iItemIngredients = 0; iItemIngredients < currentItem.ingredients.Length; iItemIngredients++)
                            {
                                currentLevel.Wave[iWave].order[iOrder].listableIngredients.Add(new ListableIngredient()); // Add a new unassigned ingredient to the slot
                                currentLevel.Wave[iWave].order[iOrder].listableIngredients[iItemIngredients].attributes = new ListableIngredient.IngredientAttribute(); // add the attribute class to said blank item.
                                currentLevel.Wave[iWave].order[iOrder].listableIngredients[iItemIngredients].ingredientItem = currentItem.ingredients[iItemIngredients]; // and assign the ingredient.

                                //Debug.Log($"Added {level.Wave[iWave].order[iOrder].listableIngredients[iItemIngredients].ingredientItem}");

                                tempFoodQueue.Enqueue(currentItem.ingredients[iItemIngredients]); // add ingredients to the queue from each sub item
                            }
                            //Debug.Log($"Added {level.Wave[iWave].order[iOrder].listableIngredients.Count}");
                        }
                    }

                    // AT THE END OF THE ORDER, DEQUEUE THE INGREDIENTS INTO THEIR RESPECTIVE FIELDS!
                    for (int f = 0; f < currentLevel.Wave[iWave].order[iOrder].listableIngredients.Count; f++) // For as many open ingredient fields as there are,
                    {

                        //Debug.Log($"{level.Wave[iWave].order[iOrder].listableIngredients[f].ingredientItem = tempFoodQueue.Dequeue()}"); // fill them up with the temp queue ingredients!
                    }

                    // CLEAR THE QUEUE FOR THE NEXT ORDER.
                    tempFoodQueue.Clear();

                }
            }
            return true;
        }

        //Safety precaution.
        private void OnDisable()
        {
            ResetAllLevelOrderFields();
        }

        // Keeps LevelSO adjustments from being saved due to serialization.
        private void ResetAllLevelOrderFields()
        {
            for (int iWave = 0; iWave < currentLevel.Wave.Length; iWave++)
            {

                for (int iOrder = 0; iOrder < currentLevel.Wave[iWave].order.Length; iOrder++)
                {
                    currentLevel.Wave[iWave].order[iOrder].listableIngredients.Clear();
                    currentLevel.Wave[iWave].order[iOrder].orderStatus = OrderStatus.WaitingForQueue;
                    currentLevel.Wave[iWave].order[iOrder].ranOutOfTime = false;
                    currentLevel.Wave[iWave].order[iOrder].madeIncorrectly = false;
                    currentLevel.Wave[iWave].order[iOrder].isOrderSucess = false;
                }
            }
        }
    }
}
