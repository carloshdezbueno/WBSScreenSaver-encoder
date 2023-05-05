# WBSScreenSaver
Stego tool based on WBScreenSaver from https://github.com/wbsimms/WBSScreenSaver
It is used to encode a message on a screensaver based on the tipical DVD screensaver switching the colors

# Implementations

The main implementation for this tool is the capability to encode a message located in the desktop of the host machine with the name "screensaver_input.txt". When the color shifts to the right (From red to green, green to blue or blue to red) a 1 is encoded, when the shift is to the left, a 0 is encoded.

# How to run

You have 2 different ways to run this tool

1. You can run the file "screensaver_final.exe" and it will automatically get the file input previously setted in the Desktop
2. You can get the Visual Studio project and compile it to get the .exe by yourself
