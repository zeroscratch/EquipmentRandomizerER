# How To Use
The program should find your steam library and find the eldenring.exe path. If the progam does not: shift + right click your elden ring executable, select "Copy as path", Paste into the path text box, and remove the quotation marks on either side.

Put the seed in that you want to use for randomization, and then hit the Randomize button. When it is done randomizing, the `Bingo!` button will be activated, and clicking it will launch the game with the randomized regulation bin. If a seed is not input one will be randomly created.

# Equipment Randomizer
This application has a simple UI to ensure that all players use the same randomized seed without worry about having the same settings.
* Weapons in general are categorized by type. For example Greatswords are their own pool as are Halberds.
* Staves and seals are not randomized. This includes the Confessor and Prisoner starting classes, merchant shops, etc.
* The Smithscript weapons, Nanaya's Torch, Lamenting Visage, Rabbath's Cannon, and the Velvet Sword of St. Trina are not randomized.
* General Sorceries are randomized with sorceries and Incantations are randomized with incantations (Dragon Communion is seperated).
* Weapon allocations are unique amongst starting classes and merchants.
* As a reminder, shields are treated as weapons by the game. 

# Weapon Type Pooling
* Great Katanas are pooled with Katanas
* Light Greatswords are pooled with Greatswords
* Hand to Hand Arts are pooled with Fists
* Beast Claws are pooled with Claws
* Backhand Blades are pooled with Daggers
* Axes and Greataxes are pooled for more weapon parity
* Thrusting and Heavy Thrusting are pooled for more weapon parity
* Ranged weapons are in a pooled category (bows & light bows, greatbows & ballistas are all in the same pool). 

# Starting Classes
Starting classes are randomized: weapons, armor, stats, and spells. Class levels are fixed to 9, with stats totalling 88.
The Prisoner starts with its unrandomized staff and one sorcery. 
The Confessor starts with its unrandomized seal and one incantation.

# Powers of Remembrance
Powers of Remembrance are randomized within the remembrance shop. 
Rennala's Remembrance gifts a randomized weapon, to keep an incentive to check each remembrance for weapons.

# Dragon Communion
Dragon communion incantations are only randomized within dragon communion locations.

# Swarm of Flies Bugfix
This mod also patches the AtkParamPC to fix a bug with SpEffectAtkPowerCorrectRate that caused swarm of flies damage to be incorrectly calculated in some circumstances. 

# Smithing Stone Cost
Smithing stone cost is also patched to be 3x stones per level for stones [1, 2, 3], 2x stones per level for stones [4, 5, 6], and 1x per level for each level after.

# Unlocked Maps
All maps are unlocked at the start of the game.

# Acknowledgements
* Big thank you to Nordgaren for being the original developer for the randomizer and helping with bug-fixing class messages post-DLC.
* All current changes to SoulsFormats in the project are SoulsFormatsNext.

# Libraries
* [Andre](https://github.com/soulsmods/DSMapStudio/blob/master/src/Andre/Andre.Formats/Param.cs) Formerly FSParam and StudioUtils, a library for parsing FromSoft param formats. These libraries are under the MIT license.  
* [SoulsFormats](https://github.com/soulsmods/DSMapStudio/tree/master/src/Andre/SoulsFormats) from the `souldmods/DSMapStudio` repo. This is a version of [SoulsFormats](https://github.com/JKAnderson/SoulsFormats) updated for .Net 6.
[SoulsFormatsNext](https://github.com/soulsmods/SoulsFormatsNEXT/)
