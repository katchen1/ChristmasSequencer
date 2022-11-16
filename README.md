# ChristmasSequencer
🎄Make music and decorate Christmas trees with an interactive audiovisual step sequencer!

## Table of Contents
1. [Video Demo](#video)
2. [Screenshots](#screenshots)
3. [Instructions on Using the Sequencer](#instructions)
4. [Production Build](#build)
5. [Ideas, Inspirations, and Comments](#comments)
6. [Acknowledgements](#acknowledgements)

## Video Demo
https://youtu.be/tdWdm5IhWcs


## Screenshots
![image](https://user-images.githubusercontent.com/59420335/201619969-532130a6-c530-4db5-9a1d-21ef3f1fe4ba.png)
![image](https://user-images.githubusercontent.com/59420335/201620010-9ca2929c-116f-4b58-b61f-5f40e89d5d56.png)

## Instructions on Using the Sequencer
- Click `Edit` to zoom in to edit a tree
- Click `Back` to zoom out
- Press `W/A/S/D` to move selector
- Press `Up/Down` to change **pitch** (vertical position of the ornament)
- Press `Left/Right` to change **volume** (width of the ornament)
- Press `Space` to create/destroy a decoration
- Press `E` to change the **instrument** of the track
    - Bells, Piano (High), Piano (Low), Drums
- Press `V/B` to change **tempo** (rate of snowfall)
- Trees default to not playing if they have no ornaments
    - Number of active trees changes **number of steps** (0, 8, 16, 24)
- Animations
    - Floating ornaments
    - Falling snow
    - Rotating stars when the tree is active

        
## Production Build
https://drive.google.com/file/d/1IM4saghCw59WKJcDJ8iO6rkDR2HDQLR8/view?usp=share_link

1. Please run on the macOS platform.
2. Download the build file and save it to a local folder.
3. Right click on the file → click “Open”.
4. If you run into the “application cannot be opened” error, set the executable flag by running `chmod -R +x <app name>.app/Contents/MacOS` in the terminal, then try opening the file again.

## Ideas, Inspirations, and Comments
This sequencer was inspired by the festive atmosphere at the end of the year, and the fact that I enjoy decorating Christmas trees. The most difficult part was converting from a single-track sequencer (as I practiced in the Chickencer tutorial project) to a multi-track sequencer, where each track produces a different configurable sound. I also spent some time wrapping my head around how Unity and Chuck communicate with each other. Another difficult part was controlling the number of steps in the sequencer according to how many and which trees are active (have at least one ornament). The part that I enjoyed the most was creating a visually appealing and flexible interface, as well as making Christmas songs with the final product.

## Acknowledgements
* Christmas decoration assets: https://assetstore.unity.com/packages/2d/textures-materials/christmas-tree-decorator-assets-50801
* Instrument sound samples downloaded from https://samplefocus.com/
