# MergeMeals
This is a project I am working on with a friend in another state. While we actually use PlasticSCM for remote collaboration, I've set up this repository for the purpose of sharing my code with any interested parties.

To learn more about this game visit [Merge: Meals](https://daren-stottrup.notion.site/Merge-Meals-1f50444c94a7426ebe59e2c6b81f927e) on my [portfolio](https://daren-stottrup.notion.site/Game-Portfolio-3bc5aac8cfcb4d32af26f20301371155).

To read about what I'm most proud of with this project see: [OrderManager.cs](#ordermanagercs) and [Level.cs](#levelcs).

## Script Rundown
The game starts with [LevelManager.cs](Assets/Scripts/Managers/LevelManager.cs) telling the other manager scripts to begin initializing their settings, and sets up the camera to fit the boardsize. [FloorManager.cs](Assets/Scripts/Managers/FloorManager.cs) grabs the level data from the prebuilt [Level.cs](Assets/Scripts/Managers/Level.cs) scriptable object, and [MergeManager](Assets/Scripts/Managers/MergeManager.cs) learns the coordinates of the board.

### Level.cs
[Level.cs](Assets/Scripts/Managers/Level.cs) is something I'm really proud of. It's built so a level designer can easily go in and design the flow of the level. There are Waves and there are Orders. The designer starts with Wave 0 and adds a customer order. Within that order, he can design what and how many [FoodItem.cs (scriptable object)](Assets/Scripts/FoodItems/FoodItem.cs) entries will be part of that order.

***For example:***
<br>
Wave 0 might contain three customer orders. Order 0, 1, and 2. Order 0 will contain a Burger, Order 1 will contain Sushi, and Order 2 will contain a taco. Alternatively, maybe Order 2 contains a taco and a burrito. The designer can then go on to build the next wave.

These Waves and Orders become known internally as Wave.Orders, and can be assigned a shorthand Vector2Int called "orderID" for communicating with other script. So the above example of a taco and burrito would be referenced by orderID: "0,2". This is used heavily in [OrderManager.cs](Assets/Scripts/Managers/OrderManager.cs).

To see an image of the Level.cs Scriptable Object in the Unity Inspector, visit my [portfolio page here](https://daren-stottrup.notion.site/Scriptable-Object-Level-cs-d7e79daad0264a55b00f292dc9a150c3).

## OrderManager.cs
The big daddy script, the one-ring to rule them all so to speak, is [OrderManager.cs](Assets/Scripts/Managers/OrderManager.cs). Admittedly, it's a bit too long for my taste and will need to be refactored into multiple scripts, but for now it's one script that does a lot.

Right away, it looks at the level and all the customer orders that are going to come in, and [builds out an ingredient list](https://github.com/dangerdaren/MergeMeals/blob/a9d0cbafbe44d5938ed04e2b398cf9bd9739cebd/Assets/Scripts/Managers/OrderManager.cs#L605-L700) for every order.

OrderManager's job is to keep track of the status of every one of these Wave.Orders. It [queues up](https://github.com/dangerdaren/MergeMeals/blob/a9d0cbafbe44d5938ed04e2b398cf9bd9739cebd/Assets/Scripts/Managers/OrderManager.cs#L153-L196) the ingredients needed for the next order and tells the [SpawnManager](Assets/Scripts/Managers/SpawnManager.cs) to drop those ingredients on the board. At the same time, it enables one of six [UIOrderUp.cs](Assets/Scripts/UI/UIOrderUp.cs) graphical tags to appear, which display the customer's order, as well as the recipe (depending on the difficulty level). For testing purposes, it also displays the orderID.

These order tags will appear at a fixed interval of time, pushing the one before it down the line. Depending on the difficulty setting, the game is more or less lenient on allowing the player to retry failed orders.

Another thing I'm very proud of regarding the OrderManager is the implementation of multipurpose functions that I wrote to keep from rewriting code again and again.


### Multipurpose Functions:
• **[GetSetOrderStatus](https://github.com/dangerdaren/MergeMeals/blob/a9d0cbafbe44d5938ed04e2b398cf9bd9739cebd/Assets/Scripts/Managers/OrderManager.cs#L499-L602)**
<br>Takes in a Wave.Order (not an orderID in this case), as well as a boolean value. "True" tells the function to analyze the order's current status, adjust it accordingly, then return the new status; whereas "False" simply returns the current status.

• **[GetWaveOrder](https://github.com/dangerdaren/MergeMeals/blob/a9d0cbafbe44d5938ed04e2b398cf9bd9739cebd/Assets/Scripts/Managers/OrderManager.cs#L291-L300)**
<br>Need to call the function above, but only have the orderID? Rather than iterate through each time, just call this function and it will return the orderIDs actual Wave.Order. So to implement it with the previously mentioned funtion, simply type "GetSetOrderStatus(GetWaveOrder(currentOrderID));" Done.

• **[GetActiveOrderTag](https://github.com/dangerdaren/MergeMeals/blob/a9d0cbafbe44d5938ed04e2b398cf9bd9739cebd/Assets/Scripts/Managers/OrderManager.cs#L273-L289)**
<br>When you need to access or modify the graphical order tag to make it disappear upon completion, or return for another attempt, you can simply provide this function the orderID as an argument, and it will return the element you're looking for.

There are more, but these are the ones that come in very hand, quite often.

## Scriptable Objects
If you're looking for some idea of how I use Scriptable Objects as reusable templates, I'd recommend (in this order):
<br> - [Level.cs](Assets/Scripts/Managers/Level.cs)  <=*Scriptable Object*
<br> - [Wave.cs](Assets/Scripts/Managers/Wave.cs)
<br> - [FoodItem.cs](Assets/Scripts/FoodItems/FoodItem.cs) <=*Scriptable Object*
<br> - [ListableIngredient.cs](Assets/Scripts/FoodItems/ListableIngredient.cs)
<br>
You can see how they are nested together.

And finally, to see the other manager scripts, you can check out the [Managers folder here](https://github.com/dangerdaren/MergeMeals/tree/master/Assets/Scripts/Managers).


## Please Note
This is very much a snapshot-in-time of a work in progress. As mentioned at the top, we use PlasicSCM for our actual version control and collaboration, so the current uploaded coded here may not reflect where the actual game is at the moment.

-Daren
