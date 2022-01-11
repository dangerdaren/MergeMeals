# MergeMeals
This is a project I am working on with a friend in another state. While we actually use PlasticSCM for remote collaboration, I've set up this repository for the purpose of sharing my code with any interested parties.

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
<br> - [Level.cs](Assets/Scripts/Managers/Level.cs)  <=*This is the actual Scriptable Object that references the following scripts.*
<br> - [Wave.cs](Assets/Scripts/Managers/Wave.cs)
<br> - [FoodItem.cs](Assets/Scripts/FoodItems/FoodItem.cs)
<br> - [ListableIngredient.cs](Assets/Scripts/FoodItems/ListableIngredient.cs)
<br>
You can see how they are nested together.

## Please Note
This is very much a snapshot-in-time of a work in progress. As mentioned at the top, we use PlasicSCM for our actual version control and collaboration, so the current uploaded coded here may not reflect where the actual game is at the moment.

Let me know if you have any questions!
<br>
-Daren
