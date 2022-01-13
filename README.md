# MergeMeals
This is a project I am working on with a friend in another state. While we actually use PlasticSCM for remote collaboration, I've set up this repository for the purpose of sharing my code with any interested parties.

To learn more about this game visit [Merge: Meals](https://daren-stottrup.notion.site/Merge-Meals-1f50444c94a7426ebe59e2c6b81f927e) on my [portfolio](https://daren-stottrup.notion.site/Past-Current-Projects-3bc5aac8cfcb4d32af26f20301371155).

## Script Owners
Most of the scripts are mine, with the following exceptions:
*<br> - MergeManager.cs*
*<br> - TouchManager.cs*
*<br> - PickUpScript.cs*
*<br> - FoodItem.cs*
*<br> - CoroutineWithData.cs*
<br><br>
Outside of those, the rest should be mine.
<br>
If you are looking for the most comprehensive script in this repository that I've written, that would be [OrderManager.cs](Assets/Scripts/Managers/OrderManager.cs), as outside of the actual merging operations, it does the legwork for most other aspects of the game. You can also see most of the other scripts it interacts with in the [Managers folder](https://github.com/dangerdaren/MergeMeals/tree/master/Assets/Scripts/Managers).

## Scriptable Objects
Finally, if you're looking for some idea of how I use Scriptable Objects as reusable templates, I'd recommend (in this order):
<br> - [Level.cs](Assets/Scripts/Managers/Level.cs)  <=*Scriptable Object*
<br> - [Wave.cs](Assets/Scripts/Managers/Wave.cs)
<br> - [FoodItem.cs](Assets/Scripts/FoodItems/FoodItem.cs) <=*Scriptable Object*
<br> - [ListableIngredient.cs](Assets/Scripts/FoodItems/ListableIngredient.cs)
<br>
You can see how they are nested together.

## Please Note
This is very much a snapshot-in-time of a work in progress. As mentioned at the top, we use PlasicSCM for our actual version control and collaboration, so the current uploaded coded here may not reflect where the actual game is at the moment.

Let me know if you have any questions!
<br>
-Daren
