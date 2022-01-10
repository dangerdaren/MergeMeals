using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using Random = UnityEngine.Random;


namespace MergeMeals
{
    public class MergeManager : MonoBehaviour
    {
        [Header("BOARD")]
        [SerializeField] private GameObject floorManagerObj;
        private int height = 0;
        private int width = 0;

        [Header("MATCH FINDING")]
        private GameObject[,] allItemsOnBoard;
        [SerializeField] private LayerMask whatIsIngredient, whatIsIngredientHeldOrNot;
        private Queue<List<GameObject>> foodItemGroupsQueue;
        [SerializeField] UnityEvent<GameObject, GameObject, List<Vector2Int>> orderMerged;

        [Header("PLAYER INPUT")]
        public GameObject lastGrabbedObj; //Needs to be public, used by the TouchManager
        [SerializeField] private GameObject lastSpawnedObj;

        [Header("PRE-MATCH FINDING")]
        [SerializeField] private GameObject blockerPrefab;
        [SerializeField] float mergeResultDelay = 0.75f; // Increase this if needed in a larger board
        [SerializeField] float killTimerSeconds = 0.75f;


        public void InitializeBoardSize(int width, int height)   //Called by LevelManager
        {
            this.height = height;
            this.width = width;
        }
        void Update()
        {
            PhysicallyScanBoard();
            FindPairsWithSameCreatesItem();
            GroupPairsWithSameCreatesItem();
            GroupedPairsMadeDistinct();
            IsLocationBelowHeldObjectVacant();
            RoleCall();
            PreMergeHighlightGroup();
            UnHighlightAllNonGroupedFoodItems();
            MergeGroups();
        }
        void PhysicallyScanBoard()
        {
            allItemsOnBoard = new GameObject[width, height];    //Resets allItemsOnBoard[,]
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector3 center = new Vector3(i, 0.5f, j);
                    Collider[] hitColliders = Physics.OverlapSphere(center, 0.2f, whatIsIngredient);
                    hitColliders = Physics.OverlapSphere(center, 0.2f, whatIsIngredientHeldOrNot);
                    if (hitColliders.Length > 1)    //There are two items on a single square, one is held.
                    {
                        GameObject itemOne = hitColliders[0].gameObject;
                        GameObject itemTwo = hitColliders[1].gameObject;
                        if (itemOne != null && itemOne.GetInstanceID() == lastGrabbedObj.GetInstanceID())
                        {
                            allItemsOnBoard[i,j] = itemTwo;
                        }
                        if (itemTwo != null && itemTwo.GetInstanceID() == lastGrabbedObj.GetInstanceID())
                        {
                            allItemsOnBoard[i,j] = itemOne;
                        }
                    }
                    if (hitColliders.Length == 1)    //There is only ont item on the single square. Grab it.
                    {
                        if (hitColliders[0].gameObject != null)
                        {
                            allItemsOnBoard[i,j] = hitColliders[0].gameObject;
                        }
                    }
                }
            }
        }
        void FindPairsWithSameCreatesItem()
        {
            foodItemGroupsQueue = new Queue<List<GameObject>>();  //Resets foodItemGroupsQueue
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    AddPairsToFoodItemGroupsQueue(i,j,1,0);       //Vert
                    AddPairsToFoodItemGroupsQueue(i,j,0,1);       //Horz
                }
            }
        }
        void AddPairsToFoodItemGroupsQueue(int i, int j, int iPos, int jPos)
        {
            List<GameObject> myPair = new List<GameObject>();
            if ((i+iPos < width) && (j+jPos < height))
            {
                if (allItemsOnBoard[i,j] != null && allItemsOnBoard[i+iPos,j+jPos] != null)
                {
                    if (IsMatch(allItemsOnBoard[i,j], allItemsOnBoard[i+iPos,j+jPos]))
                    {
                        PickUpScript one = allItemsOnBoard[i,j].GetComponent<PickUpScript>();
                        PickUpScript two = allItemsOnBoard[i+iPos,j+jPos].GetComponent<PickUpScript>();
                        //if (one.canBeMatched && two.canBeMatched)   //Commenting this causes the flashing to stop
                        {
                            if (!one.IsMoving() && !two.IsMoving()) //Only adds items that are not in the process of moving
                            {
                                myPair.Add(allItemsOnBoard[i,j]);
                                myPair.Add(allItemsOnBoard[i+iPos,j+jPos]);
                                foodItemGroupsQueue.Enqueue(myPair);
                            }
                        }
                    }
                }
            }
        }
        bool IsMatch(GameObject one, GameObject two)
        {
            return SharesCommonCreatesFoodItem(one, two);
        }
        void GroupPairsWithSameCreatesItem()
        {
            //Makes a copy of the anims queue
            Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
            foodItemGroupsQueue = new Queue<List<GameObject>>();    //Refreshed foodItemGroupsQueue
            if (tempQueue != null)
            {
                bool recheck = true;
                while (tempQueue.Count > 0)
                {
                    List<GameObject> firstList = tempQueue.Dequeue();
                    while (recheck)
                    {
                        recheck = false;
                        for (int i = 0; i < tempQueue.Count; i++)
                        {
                            List<GameObject> nextList = tempQueue.Dequeue();
                            if (HasCommonElements(firstList, nextList))
                            {
                                firstList = firstList.Union(nextList).ToList();
                                recheck = true;
                            }
                            else
                            {
                                tempQueue.Enqueue(nextList);
                            }
                        }
                    }
                    if (firstList.Count > 2)
                    {
                        foodItemGroupsQueue.Enqueue(firstList);
                    }
                    recheck = true;
                }
            }
        }
        //This function dumps the foodItemGroupsQueue and checks if the item under the held object is vacant
        void IsLocationBelowHeldObjectVacant()
        {
            if (foodItemGroupsQueue != null)
            {
                Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
                foodItemGroupsQueue = new Queue<List<GameObject>>();      //Resets foodItemGroupsQueue
                if (tempQueue != null)
                {
                    for (int i = 0; i < tempQueue.Count; i++)
                    {
                        List<GameObject> groupList = tempQueue.Dequeue();
                        if (lastGrabbedObj != null && lastGrabbedObj.GetComponent<PickUpScript>().fingerPressed)
                        {
                            int grabX = (int) Mathf.RoundToInt(lastGrabbedObj.transform.position.x);
                            int grabZ = (int) Mathf.RoundToInt(lastGrabbedObj.transform.position.z);
                            GameObject itemAtLocation = allItemsOnBoard[grabX,grabZ];
                            if (itemAtLocation != null)
                            {
                                bool itemBelowSameAsHeldObject = itemAtLocation.GetInstanceID() == lastGrabbedObj.GetInstanceID();
                                bool itemBelowIsADudPlacementMarker = lastGrabbedObj.GetComponent<PickUpScript>().GetPlaceHolderID() == itemAtLocation.GetInstanceID();
                                if (itemBelowSameAsHeldObject || itemBelowIsADudPlacementMarker)
                                {
                                    foodItemGroupsQueue.Enqueue(groupList);     //Reinserts back into foodItemGroupsQueue if IsLocationBelowHeldObjectVacant passes
                                }
                            }
                        }
                        else
                        {
                            foodItemGroupsQueue.Enqueue(groupList);             //Finger is released, bypass "IsLocationBelow" checks
                        }
                    }
                }
            }
        }
        //Dequeues a list, finds common createsItem, confirms that at least one of each element is present in the list, if so enqueues it back
        void RoleCall()
        {
            if (foodItemGroupsQueue != null)
            {
                Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
                foodItemGroupsQueue = new Queue<List<GameObject>>();      //Resets foodItemGroupsQueue
                if (tempQueue != null)
                {
                    for (int i = 0; i < tempQueue.Count; i++)
                    {
                        List<GameObject> groupList = tempQueue.Dequeue();
                        FoodItem groupsCreatesItem = GetCommonCreatesFoodItem(groupList);
                        if (groupsCreatesItem != null)
                        {
                            Dictionary<FoodItem, int> itemIventoryInGroup = MakeCountDictionary(groupList);
                            FoodItem[] ingredients = groupsCreatesItem.ingredients;
                            bool allItemsPresent = true;
                            for (int j = 0; j < ingredients.Length; j++)
                            {
                                if (!itemIventoryInGroup.ContainsKey(ingredients[j]))
                                {
                                    allItemsPresent = false;
                                }
                            }
                            if (allItemsPresent)
                            {
                                foodItemGroupsQueue.Enqueue(groupList);       //Reinserts back into foodItemGroupsQueue if roleCall passes
                            }
                        }
                    }
                }
            }
        }
        //Finds all objects that aren't in a group, and removes their animations
        void UnHighlightAllNonGroupedFoodItems()
        {
            //If there's nothing in the queue, just clear out the animation for the object at [i,j]
            if (foodItemGroupsQueue != null && foodItemGroupsQueue.Count <= 0)
            {
                ResetAnimForAll();
            }
            else //Makes a copy of the foodItemGroupsQueue for each paired element in the 2D boarde
            {
                Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
                if (tempQueue != null)
                {
                    while (tempQueue.Count > 0)
                    {  //Loops through all groups (should only ever be 1)
                        List<GameObject> nextList = tempQueue.Dequeue();
                        //For each list, 2d loop through the entire board
                        for (int i = 0; i < width; i++)
                        {
                            for (int j = 0; j < height; j++)
                            {
                                if (allItemsOnBoard[i,j] != null)
                                {
                                    //Loops through (potentially) the entire list for each 2d element on the board
                                    if (!HasCommonElements(allItemsOnBoard[i,j], nextList))
                                    {
                                        ResetAnimForObj(allItemsOnBoard[i,j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void ResetAnimForAll()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (allItemsOnBoard[i,j] != null)
                    {
                        ResetAnimForObj(allItemsOnBoard[i,j]);
                    }
                }
            }
        }
        void ResetAnimForObj(GameObject obj)
        {
            PickUpScript myObj = obj.GetComponent<PickUpScript>();
            //if (myObj.canBeMatched)
            {
                myObj.canBeMatched = true;
                myObj.mySquare.enabled = false;
                myObj.aboutToBeMatch = false;
            }
        }
        bool HasCommonElements(List<GameObject> firstList, List<GameObject> secondList)
        {
            for (int i = 0; i < firstList.Count; ++i)
            {
                for (int j = 0; j < secondList.Count; ++j)
                {
                    if (firstList.ElementAt(i) == secondList.ElementAt(j))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        bool HasCommonElements(GameObject obj, List<GameObject> myList)
        {
            for (int i = 0; i < myList.Count; ++i)
            {
                if (obj.GetInstanceID() == myList.ElementAt(i).GetInstanceID())
                {
                    return true;
                }
            }
            return false;
        }
        GameObject FindFoodItemBeingCarried(List<GameObject> myList)
        {
            for (int i = 0; i < myList.Count; i++)
            {
                if (myList[i] != null)
                {
                    PickUpScript itemScript = myList[i].GetComponent<PickUpScript>();
                    if (itemScript.CurrentlyBeingCarried())
                    {
                        return itemScript.gameObject;
                    }
                }
            }
            return null;
        }
        // This is when all the ingredients are moved towards the
        // dropped item (that was just recently being held).
        // The argument list is the just dequeued prefabs instances (ingredients).
        void MoveElementsTowardsFocusObject(List<GameObject> ingredientList)
        {
            List<Vector2Int> orderIDList = new List<Vector2Int>();

            if (FindFoodItemBeingCarried(ingredientList) == null && ingredientList.Count > 2)
            {
                for (int i = 0; i < ingredientList.Count; i++)
                {
                    if (ingredientList[i] != null)
                    {
                        PickUpScript itemScript = ingredientList[i].GetComponent<PickUpScript>();
                        if (!itemScript.aboutToBeMatch)
                        {
                            itemScript.mySquare.enabled = false;
                            itemScript.aboutToBeMatch = true;
                            itemScript.canBeMatched = false;
                            ingredientList[i].layer = 9;    //Resets to "Ingredients" layer to prevent from triggering future matches?
                            //Only moves the fooditem not being held by the user
                            if (ingredientList[i].GetInstanceID() != lastGrabbedObj.GetInstanceID())
                            {
                                itemScript.MoveToLocation(lastGrabbedObj.transform.position);
                            }

                            orderIDList.Add(ingredientList[i].GetComponent<IngredientTypeScript>().waveOrderID); // Add the orderID to the orderIDList.

                            itemScript.KillTimer(killTimerSeconds);
                        }
                    }


                }
                //orderMerged.Invoke(UIElementGameObject, DraggedFoodItemGameObject, orderIDList); //Daren (bookmark)
                FoodItem groupsCreatesItem = GetCommonCreatesFoodItem(ingredientList);
                if (groupsCreatesItem != null)
                {
                    Instantiate(groupsCreatesItem.itemPrefab, lastGrabbedObj.transform.position, Quaternion.identity);
                }
            }
        }
        /*
        PickUpScript FindHeldObject(List<GameObject> myList)
        {
            for (int i = 0; i < myList.Count; i++)
            {
                PickUpScript myObj = myList[i].GetComponent<PickUpScript>();
                if (myObj.CurrentlyBeingCarried())
                {
                    return myObj;
                }
            }
            return null;
        }
        */
        public bool IsWithinBounds(Vector3 pos)
        {
            if (pos.x >= 0 && pos.x < width && pos.z >= 0 && pos.z < height) { //bounds check
                return true;
            }
            return false;
        }
        //This function tells all items that the player's finger is being pressed
        public void FingerPressed()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (allItemsOnBoard[i,j] != null)
                    {
                        allItemsOnBoard[i,j].GetComponent<PickUpScript>().fingerPressed = true;
                    }
                }
            }
        }
        //This function sets all items to the grounded layer and turns off the white border
        public void FingerReleased()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (allItemsOnBoard[i,j] != null)
                    {
                        allItemsOnBoard[i,j].gameObject.layer = 6;  //6 is the grounded layer
                        allItemsOnBoard[i,j].GetComponent<PickUpScript>().fingerPressed = false;
                    }
                }
            }
        }
        //TODO this might be used if there is a 2x, 3x, or something like that later on in the game
        bool AllButOneIsADuplicate(Dictionary<FoodItem, int> itemIventory)
        {
            int countOfItemsWithDuplicates = 0;
            foreach(KeyValuePair<FoodItem, int> kvp in itemIventory)
            {
                if (kvp.Value > 1)
                {
                    countOfItemsWithDuplicates++;
                }
            }
            return (itemIventory.Count - countOfItemsWithDuplicates) == 1;
        }
        //Since we're only removing one single FoodItem from a pre-merged group at a time,
        //We can focus our attention on a single FoodItem type in each iteration
        FoodItem GetFirstDuplicatedFoodItemType(Dictionary<FoodItem, int> itemIventory)
        {
            foreach(KeyValuePair<FoodItem, int> kvp in itemIventory)
            {
                if (kvp.Value > 1)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        //Removes the duplicates which is farthest away from the held object
        GameObject GetDupeByDistance(List<GameObject> groupList, FoodItem dupeFoodItemType, GameObject focusObj)
        {
            float farthestDistance = 0.0f;
            int farCount = 0;
            if (focusObj != null)
            {
                Vector3 heldPos = focusObj.transform.position;
                //This loop gets the farthest ditance and the number of items that have that distance
                for (int i = 0; i < groupList.Count; i++)
                {
                    FoodItem nextFoodItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                    if (nextFoodItem == dupeFoodItemType)
                    {
                        Vector3 nextDupePos = groupList[i].transform.position;
                        float distance = Vector3.Distance(heldPos, nextDupePos);
                        if (distance <= 0.05f)  //Means they're both really close
                        {
                            farCount++;         //Means we have more than one with the same distance
                        }
                        else                    //Update farthestDistance to the farthestDistance
                        {
                            farthestDistance = Mathf.Max(farthestDistance, distance);
                        }
                    }
                }
                if (farCount <= 1)   //This means we DO NOT have more than one duplicate at equal distance from the object being held
                {
                    //Now that we have the farthest distance, we compare other duplicates to that distance
                    for (int i = 0; i < groupList.Count; i++)
                    {
                        FoodItem nextFoodItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                        if (nextFoodItem == dupeFoodItemType)
                        {
                            Vector3 nextDupePos = groupList[i].transform.position;
                            float distance = Vector3.Distance(heldPos, nextDupePos);
                            if (distance == farthestDistance)
                            {
                                return groupList[i];
                            }
                        }
                    }
                }
            }
            return null;
        }
        //There are duplicate FoodItems, they have equal connections and distance from held item.
        //Removes from the duplicates from the pre-merge group  which is a dudPlacementMarker
        GameObject GetDupeThatsAlsoADud(List<GameObject> groupList, FoodItem dupeFoodItemType, GameObject focusObj)
        {
            if (focusObj != null)
            {
                if (focusObj.GetComponent<IngredientTypeScript>().foodItem == dupeFoodItemType)
                {
                    return focusObj;
                }
            }
            return null;
        }
        //There are duplicate FoodItems, they have equal connections and distance from held item.
        //This is the last chance, removes duplicates if they're diagonal from the held object first, then NESW.
        GameObject GetDupeByPosition(List<GameObject> groupList, FoodItem dupeFoodItemType, GameObject focusObj)
        {
            if (focusObj != null)
            {
                Vector3 heldPos = focusObj.transform.position;
                for (int i = 0; i < groupList.Count; i++)
                {
                    FoodItem nextFoodItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                    if (nextFoodItem == dupeFoodItemType)
                    {
                        Vector3 nextDupePos = groupList[i].transform.position;
                        float distance = Vector3.Distance(heldPos, nextDupePos);
                        for (int j = 0; j < 10; j++)
                        {
                            if (Vector3.Distance(nextDupePos, heldPos+((Vector3.right+Vector3.forward) * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+((Vector3.right+Vector3.back) * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+((Vector3.left+Vector3.back) * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+((Vector3.left+Vector3.forward) * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+(Vector3.right * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+(Vector3.forward * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+(Vector3.left * j)) <= 0.05f)
                                return groupList[i];
                            if (Vector3.Distance(nextDupePos, heldPos+(Vector3.back * j)) <= 0.05f)
                                return groupList[i];
                        }
                    }
                }
            }
            return null;
        }
        //Loops through all elements in each queue and confirms that they're all unique
        //We use a dictionary to count the number of distinct ingredients in the group
        Dictionary<FoodItem, int> MakeCountDictionary(List<GameObject> groupList)
        {
            Dictionary<FoodItem, int> itemIventory = new Dictionary<FoodItem, int>();
            FoodItem groupsCreatesItem = GetCommonCreatesFoodItem(groupList);
            if (groupsCreatesItem != null)
            {
                for (int j = 0; j < groupsCreatesItem.ingredients.Length; j++)
                {
                    for (int k = 0; k < groupList.Count; k++)
                    {
                        FoodItem nextFoodItem = groupList[k].GetComponent<IngredientTypeScript>().foodItem;
                        if (groupsCreatesItem.ingredients[j] == nextFoodItem)
                        {
                            if (itemIventory.ContainsKey(nextFoodItem))
                            {
                                itemIventory[nextFoodItem]++;
                            }
                            else
                            {
                                itemIventory[nextFoodItem] = 1;
                            }
                        }
                    }
                }
            }
            return itemIventory;
        }
        //Iterates through all 'CreatesFoodItem's for both members and sees if they both have a common 'CreatesFoodItem'
        bool SharesCommonCreatesFoodItem(GameObject one, GameObject two)
        {
            List<GameObject> groupList = new List<GameObject>();
            groupList.Add(one);
            groupList.Add(two);
            return GetCommonCreatesFoodItem(groupList) != null;
        }
        //Gets the parent 'CreatesFoodItem' that all members in the group can make together
        FoodItem GetCommonCreatesFoodItem(List<GameObject> groupList)
        {
            Dictionary<FoodItem, int> createsItemInventory = new Dictionary<FoodItem, int>();
            for (int i = 0; i < groupList.Count; i++)
            {
                FoodItem nextItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                for (int j = 0; j < nextItem.createsItem.Length; j++)
                {
                    FoodItem nextCreatesItem = nextItem.createsItem[j];
                    if (createsItemInventory.ContainsKey(nextCreatesItem))
                    {
                        createsItemInventory[nextCreatesItem]++;
                    }
                    else
                    {
                        createsItemInventory[nextCreatesItem] = 1;
                    }
                }
            }
            //Iterates through the dictionary. If any item is the same length as the entire group, make that createsItem
            foreach(KeyValuePair<FoodItem, int> kvp in createsItemInventory)
            {
                if (kvp.Value == groupList.Count)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        //Called from the GroupedPairsMadeDistinct() function when a duplicate item needs to be removed from the pre-merge group
        List<GameObject> RemoveDuplicateFromList(List<GameObject> groupList, GameObject dupe)
        {
            List<GameObject> tempList = new List<GameObject>(groupList);
            for (int i = 0; i < tempList.Count; i++)
            {
                if (dupe.GetInstanceID() == tempList[i].GetInstanceID())
                {
                    ResetAnimForObj(groupList[i]);
                    tempList.RemoveAt(i);
                }
            }
            return tempList;
        }
        //Removes a single duplicate in a list, yet only the dupes that has the least number of adjacent neighbors
        GameObject GetDupeByConnections(List<GameObject> groupList, FoodItem dupeFoodItemType)
        {
            int maxNeighbors = GetMaxGroupieTouchesForThisFoodItemType(groupList, dupeFoodItemType);
            for (int i = 0; i < groupList.Count; i++)
            {
                FoodItem nextFoodItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                if (nextFoodItem == dupeFoodItemType)
                {
                    int nextNeighbors = GetGroupieTouchCount(groupList[i], groupList);
                    if (maxNeighbors > nextNeighbors)
                    {
                        return groupList[i];  //Returns a single item so that we for certain keep at least one.
                    }
                }
            }
            return null;
        }
        //For each duplicate foodItem type in the group, get the highest number of touches from neighboring group members
        int GetMaxGroupieTouchesForThisFoodItemType(List<GameObject> groupList, FoodItem dupeFoodItemType)
        {
            int maxNeighbors = 0;
            for (int i = 0; i < groupList.Count; i++)
            {
                FoodItem nextFoodItem = groupList[i].GetComponent<IngredientTypeScript>().foodItem;
                if (nextFoodItem == dupeFoodItemType)
                {
                    int nextNeighbors = GetGroupieTouchCount(groupList[i], groupList);
                    maxNeighbors = Mathf.Max(maxNeighbors, nextNeighbors);
                }
            }
            return maxNeighbors;
        }
        //Focuses on removing a single duplicated fooditem at a time, to confirm not too many are removed.
        //Because of this, only one single FoodItemtype is considered per iteration.
        void GroupedPairsMadeDistinct()
        {
            //Makes a copy of the pairs queue
            Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
            foodItemGroupsQueue = new Queue<List<GameObject>>();    //Resets foodItemGroupsQueue
            if (tempQueue != null)
            {
                bool recheck = true;
                for (int i = 0; i < tempQueue.Count; i++)
                {
                    for (int j = 0; recheck && j < 50; j++)    //Some arbitrary number 50 to prevent undless looping
                    {
                        recheck = false;
                        List<GameObject> groupList = tempQueue.Dequeue();
                        Dictionary<FoodItem, int> itemIventory = MakeCountDictionary(groupList);
                        // Call AllButOneIsADuplicate() if/when the 2x position on the board is being used
                        FoodItem duplicatedFoodItemType = GetFirstDuplicatedFoodItemType(itemIventory);
                        if (duplicatedFoodItemType != null)  //A duplicate FoodItem has been found
                        {
                            //if there's one dupe in the group being held, and another of the same dupe foodtype just spawned, kill the spawn.
                            GameObject dupeToKill = GetDupeByConnections(groupList,duplicatedFoodItemType);
                            if (dupeToKill != null)
                            {
                                List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                tempQueue.Enqueue(groupListWithoutDupe);
                                recheck = true;
                            }
                            else    // There are duplicate FoodItems in the group, but they all have the same amount of connections
                            {
                                if (lastGrabbedObj != null && HasCommonElements(lastGrabbedObj, groupList))    //Confirms that the bottom only applies if the lastGrabbedObj was related to this group's matching (there is the possibility that the player could be moving something while a new foodItem is randomly dropped from the sky and we don't want to do comparisons based off of that foodItem being held)
                                {
                                    dupeToKill = GetDupeByDistance(groupList, duplicatedFoodItemType, lastGrabbedObj);
                                    if (dupeToKill != null)
                                    {
                                        List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                        tempQueue.Enqueue(groupListWithoutDupe);
                                        recheck = true;
                                    }
                                    else    // There are duplicate FoodItems, they have equal connections and distance from held item
                                    {
                                        dupeToKill = GetDupeByPosition(groupList, duplicatedFoodItemType, lastGrabbedObj);
                                        if (dupeToKill != null)
                                        {
                                            List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                            tempQueue.Enqueue(groupListWithoutDupe);
                                            recheck = true;
                                        }
                                    }
                                }
                                else    //The list contains duplicate foodItems and none are being held. We need to remove the duplicates with dudPlacementMarkers
                                {
                                    GameObject spawnedDud = FindDudPlacementMarker(groupList);
                                    if (spawnedDud != null && HasCommonElements(spawnedDud, groupList))
                                    {
                                        dupeToKill = GetDupeByDistance(groupList, duplicatedFoodItemType, spawnedDud);
                                        if (dupeToKill != null)
                                        {
                                            List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                            tempQueue.Enqueue(groupListWithoutDupe);
                                            recheck = true;
                                        }
                                        else    // There are duplicate FoodItems, they have equal connections and distance from spawned item
                                        {
                                            dupeToKill = GetDupeThatsAlsoADud(groupList, duplicatedFoodItemType, spawnedDud);
                                            if (dupeToKill != null) //There are two duplicates, and one is a dudPlacementMarker, remove the dud
                                            {
                                                List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                                tempQueue.Enqueue(groupListWithoutDupe);
                                                recheck = true;
                                            }
                                            else
                                            {
                                                dupeToKill = GetDupeByPosition(groupList, duplicatedFoodItemType, spawnedDud);
                                                if (dupeToKill != null)
                                                {
                                                    List<GameObject> groupListWithoutDupe = RemoveDuplicateFromList(groupList, dupeToKill);
                                                    tempQueue.Enqueue(groupListWithoutDupe);
                                                    recheck = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else    //No duplicates, we're all done!
                        {
                            if (groupList.Count > 2)
                            {
                                foodItemGroupsQueue.Enqueue(groupList);
                            }
                        }
                    }
                }
            }
        }
        //This highlights the group, meaning, it turns on each element's white border
        void PreMergeHighlightGroup()
        {
            //Makes a copy of the foodItemGroupsQueue queue
            Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
            if (tempQueue != null)
            {
                for (int i = 0; i < tempQueue.Count; i++)
                {
                    List<GameObject> groupList = tempQueue.Dequeue();
                    if (groupList.Count > 2)
                    {
                        if (AreAllGroupMembersTouchingAfterBeingMadeDistinct(groupList))
                        {
                            if (FindAndMarkDudPlacementMarker(groupList) == null)
                            {
                                LightEmUp(groupList);
                            }
                        }
                    }
                }
            }
        }
        //This actually performs the animation of moving items to the one item last held and creatings the common parent item
        void MergeGroups()
        {
            if (foodItemGroupsQueue != null)
            {
                for (int i = 0; i < foodItemGroupsQueue.Count; i++)
                {
                    List<GameObject> groupList = foodItemGroupsQueue.Dequeue();
                    if (groupList.Count > 2)
                    {
                        if (AreAllGroupMembersTouchingAfterBeingMadeDistinct(groupList))
                        {
                            if (FindAndMarkDudPlacementMarker(groupList) == null)
                            {
                                MoveElementsTowardsFocusObject(groupList);
                            }
                        }
                    }
                }
            }
        }
        //Confirms that highlighting and merging don't actually take place with dudPlacementMarkers
        GameObject FindDudPlacementMarker(List<GameObject> groupList)
        {
            for (int j = 0; j < groupList.Count; j++)
            {
                PickUpScript myObj = groupList[j].GetComponent<PickUpScript>();
                if (myObj.AmIADudPlacementMarker())
                {
                    return myObj.gameObject;
                }
            }
            return null;
        }

        //Confirms that highlighting and merging don't actually take place with dudPlacementMarkers
        GameObject FindAndMarkDudPlacementMarker(List<GameObject> groupList)
        {
            for (int j = 0; j < groupList.Count; j++)
            {
                PickUpScript myObj = groupList[j].GetComponent<PickUpScript>();
                if (myObj.AmIADudPlacementMarker())
                {
                    myObj.IAmADudPlacementMarkerAndIMadeAMatch = true;
                    return myObj.gameObject;
                }
            }
            return null;
        }
        //Turning on the foodItem's white border square
        void LightEmUp(List<GameObject> groupList)
        {
            for (int j = 0; j < groupList.Count; j++)
            {
                PickUpScript myObj = groupList[j].GetComponent<PickUpScript>();
                if (myObj.canBeMatched)
                {
                    myObj.mySquare.enabled = true;
                    myObj.canBeMatched = false;
                }
            }
        }
        void DebugPrintAllItemsOnBoard()
        {
            for (int i = 0; i < width; i++)
            {
                string res = "";
                for (int j = 0; j < height; j++)
                {
                    if (j > 0) { res += " "; }
                    if (allItemsOnBoard[i,j]) { res += allItemsOnBoard[i,j].name; }
                    if (j < height-1) { res += ","; }
                }
                Debug.Log(res);
            }
        }
        string DebugListToString(List<GameObject> myList)
        {
            string res = "";
            for (int i = 0; i < myList.Count; i++)
            {
                if (i > 0) { res += " "; }
                res += myList[i].name;
                if (i < myList.Count-1) { res += ","; }
            }
            return res;
        }
        string DebugListToString(List<Vector3Int> myList)
        {
            string res = "";
            for (int i = 0; i < myList.Count; i++)
            {
                if (i > 0) { res += " "; }
                res += myList[i];
                if (i < myList.Count-1) { res += ","; }
            }
            return res;
        }
        IEnumerator DoesDudPlacementMarkerMakeAMatch(Vector3Int pos, FoodItem obj)
        {
            GameObject dudFoodItem = Instantiate(obj.itemPrefab, pos, Quaternion.identity);
            PickUpScript dudPU = dudFoodItem.GetComponent<PickUpScript>();
            dudPU.MakeIntoADudPlacementMarkerFoodItem();
            dudFoodItem.name = "DUD_"+dudFoodItem.name;
            dudFoodItem.GetComponent<IngredientTypeScript>().SetDisplayType("none");
            bool recheck = true;
            yield return new WaitForSeconds(mergeResultDelay);
            if (dudPU && dudPU.IAmADudPlacementMarkerAndIMadeAMatch)
            {
                Destroy(dudPU.gameObject);
                dudPU = null;
                recheck = false;
                yield return true;  //Yes, this dudPlacementMarker made a match
            }
            if (dudPU)  //Clean up
            {
                Destroy(dudPU.gameObject);
                dudPU = null;
                yield return false;     //No, this dudPlacementMarker did not make a match
            }
            //yield return null;
        }
        bool AreAllGroupMembersTouchingAfterBeingMadeDistinct(List<GameObject> groupList)
        {
            int groupieTouchCount = 0;
            for (int i = 0; i < groupList.Count; i++)
            {
                groupieTouchCount += GetGroupieTouchCount(groupList[i], groupList);
            }
            return groupieTouchCount >= groupList.Count;
        }
        //Returns the number of members in the same group are touching the specific obj passed in the first argument
        int GetGroupieTouchCount(GameObject firstObj, List<GameObject> groupList)
        {
            int thisObjectsGroupieTouchCount = 0;
            for (int j = 0; j < groupList.Count; j++)
            {
                GameObject secondObj = groupList[j];
                if (firstObj.GetInstanceID() != secondObj.GetInstanceID())   //We don't consider ourself
                {
                    Vector3 groupiePos = secondObj.transform.position;
                    Vector3 thisPos = firstObj.transform.position;
                    if (Vector3.Distance(groupiePos, thisPos) <= 1.1f)  //Has to be just over 1.0 to allow for the lift of the held object. Took me a while to get that one.
                    {
                        thisObjectsGroupieTouchCount++;
                    }
                }
            }
            return thisObjectsGroupieTouchCount;
        }
        List<Vector3Int> MakeListOfAvailableLocations()
        {
            List<Vector3Int> availableLocations = new List<Vector3Int>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (allItemsOnBoard[i,j] == null)
                    {
                        Vector3Int location = new Vector3Int(i, 0, j);
                        availableLocations.Add(location);
                    }
                }
            }
            return availableLocations;
        }
        public IEnumerator GetVacantAndNonMergingLocation(FoodItem foodItem)    //Called from SpawnManager
        {

            bool recheck = true;
            lastSpawnedObj = foodItem.itemPrefab;
            //This needs to be a while loop, should be safe from the coroutines
            //for (int i = 0; recheck && i < availableLocations.Count; i++)
            while (recheck)
            {
                List<Vector3Int> availableLocations = MakeListOfAvailableLocations();
                if (availableLocations.Count > 1)
                {
                    //Randomly selects a vector from the list
                    int index = Random.Range(0,availableLocations.Count);
                    Vector3Int pos = availableLocations[index];
                    //Drops a dud in that location to see if a match takes place
                    CoroutineWithData cd = new CoroutineWithData(this, DoesDudPlacementMarkerMakeAMatch(pos,foodItem));
                    yield return cd.coroutine;
                    if (cd.result is bool)    //Since the CoroutineWithData() can return any class type, this confirms its a bool
                    {
                        if ((bool)cd.result)  //If a match was found from the dudPlacementMarker, remove that location from the list and try again.
                        {
                            availableLocations.RemoveAt(index);
                        }
                        else
                        {
                            CreateDudPlacementMarker(pos.x,pos.z);
                            recheck = false;
                            yield return new Vector3Int(pos.x, 5, pos.z);
                        }
                    }
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        void CreateDudPlacementMarker(int myX, int myZ)
        {
            Vector3 pos = new Vector3(myX, 0.2f, myZ);
            GameObject blocker = Instantiate(blockerPrefab, pos, Quaternion.identity);
            //TODO This needs to NOT be on a timer, but instead get removed once a piece is placed
            blocker.GetComponent<PickUpScript>().KillTimer(1.2f);   //Takes this long to reach the ground
        }
        void DebugPrintQueue()
        {
            if (foodItemGroupsQueue != null)
            {
                Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
                foodItemGroupsQueue = new Queue<List<GameObject>>();      //Resets foodItemGroupsQueue
                if (tempQueue != null)
                {
                    for (int i = 0; i < tempQueue.Count; i++)
                    {
                        List<GameObject> groupList = tempQueue.Dequeue();
                        Debug.Log(DebugListToString(groupList));
                        foodItemGroupsQueue.Enqueue(groupList);
                    }
                }
            }
        }
        /*  OLD ROLECALL() LEAVING JUST INCASE THERE ARE FUTURE ISSUES WITH THE NEW ONE (10/22/21)
        //Dequeues a list, finds common createsItem, confirms that at least one of each element is present in the list, if so enqueues it back
        void RoleCall()
        {
            if (foodItemGroupsQueue != null)
            {
                Queue<List<GameObject>> tempQueue = new Queue<List<GameObject>>(foodItemGroupsQueue);
                foodItemGroupsQueue = new Queue<List<GameObject>>();      //Resets foodItemGroupsQueue
                if (tempQueue != null)
                {
                    for (int i = 0; i < tempQueue.Count; i++)
                    {
                        List<GameObject> groupList = tempQueue.Dequeue();
                        FoodItem groupsCreatesItem = GetCommonCreatesFoodItem(groupList);
                        FoodItem[] allIngredients = groupsCreatesItem.ingredients;
                        bool allItemsPresent = true;
                        for (int j = 0; j < allIngredients.Length; j++)
                        {
                            if (!ListContainsFoodItem(groupList,allIngredients[j]))
                            {
                                Debug.Log(allIngredients[j].itemPrefab.gameObject.name+" IS NOT IN THE DICT");
                                allItemsPresent = false;
                            }
                            else
                            {
                                Debug.Log(allIngredients[j].itemPrefab.gameObject.name+" IS IN THE DICT");
                            }

                        }
                        if (allItemsPresent)
                        {
                            foodItemGroupsQueue.Enqueue(groupList);       //Reinserts back into foodItemGroupsQueue if roleCall passes
                        }
                    }
                }
            }
        }
        bool ListContainsFoodItem(List<GameObject> groupList, FoodItem item)
        {
            for (int i = 0; i < groupList.Count; i++)
            {
                if (item.itemPrefab.gameObject.name == groupList[i].name)
                {
                    return true;
                }
            }
            return false;
        }
        */
    }
}
