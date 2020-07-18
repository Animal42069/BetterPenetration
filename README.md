# BetterPenetration
This plugin seeks to solve to "telescoping" issue that occurs in HScenes in AI Shoujo and Honey Select 2.  It also adds some additional features to overall improve the HScene experience.  This plugin does not work for studio.

# Features
Replaces the default "telescoping" behavior by allowing the head to move past the point of penetration.<br>
Two methods to reposition the head of the penis.  The first will maintain penis length, but change rotation to find a spot that doesn't clip.<br>
The second method will telescope the penis just enough to prevent clipping.<br>
Works for vaginal, anal, and oral penetraion.<br>
Maintain proper rotation of the penis, no more spinning shafts during certain positions.<br>
Adjust the length and girth of penis.<br>
Adjust the overall sack size.<br>
Can add softness to the penis, to add a certain amount of squishiness after penetration.<br>
Offset options to further tweak things when using characters with abnormal body shapes.<br>
Supporst multiple male and multiple female positions present in Honey Select 2.<br>

# Notes
This plugin does not work for studio.  I have a few ideas on how to make something similar work in studio, but it would be a major undertaking.<br>
Male uncensor requires a shaft bone and a head bone, which most uncensors have.<br>
No special female uncensors are needed, but if you use Roy12's vagina uncensor it will utilize the dynamic bones if they are present.<br>
The mod works by keeping track of certain bones on the girl and using that information to set boundaries. Any character made in the game will have these bones. If somehow these bones aren't present then it will revert to default behavior. The mod tries to place the head inside the girl at a position that pierces the original target (vagina, anus, mouth). Due to sizes, lengths, angles and different positions this isn't always possible. It is recommended to use Mantas' BetterHScenes to adjust the characters in the scene to make the geometry involved more favorable

# Installation
Requires BepInEx<br>
Copy the dll to your install directory /BepInEx/plugins
