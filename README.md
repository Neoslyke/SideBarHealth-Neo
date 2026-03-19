# SideBarHealth

A TShock plugin that shows teammate's health values on the sidebar in text format.

You can customize the color of the health bar via the Config file, Add Keyframes to change colors depending on the HP%, Customize the format to be shown, and add a special format if the player is dead

## How to Install
1. Download the `.dll` file.
2. Put the `.dll` file inside of `/ServerPlugins/`
3. Stop and rerun the server.

## Versions
SideBarHealth v0.7.4 (Latest)

## Instructions
### Commands
`/toggleinfo`

### Permissions
`tshock.canchat`

### Config file params
The config file will appear once you have installed the plugin and ran Tshock once
You can refresh it on the go once you modified by running /refresh in the Tshock console
|Parameter|Valid values|Description|
|---|---|---|
|gradient|String array,Its a color in RGB HEX format|Sets the color for the corresponding key frame,Must have the same number of elements as the keyframes|
|gradientKeyFrames|Int array, HP% value|Represents the HP% value of which will be the color applied, so if we got 100,0 Color 1 will be used when the HP% value is 100%, and will gradually change to color2 as it gets closer to 0%HP, if its 100,100,50, it means that there will not be a gradient as only color1 wil be shown for 100%, and then color 2 will be used for the next gradient to color3, Its important to note that values must start from 120 and end to 0, 120 being the case for lifeforce potion|
|colorDamage|string, Color in RGB HEX format|The color that will be used to represent the bar segments of damage|
|format|string|The format of the text shown in the sidebar; Parameters : {0} shows the player name {1} shows the health bar, {2} shows the HP numerical value |
|barchar|char|The character used to represent a healthbar segment|
|deadFormat|string|Text shown if the player is dead ; Parameters : {0} Shows the player name|
|Outset|string|A text added at the end of the message to adjust the text horizontal position|
|ArrayLength|Byte|Not implemented yet|
|TextFlag|Byte|See Terraria multiplayer packet structure for reference ; 1= hide default message 2=Use shadows/Text outline 3=1 and 2 |