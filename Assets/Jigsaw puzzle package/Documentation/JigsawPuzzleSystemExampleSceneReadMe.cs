using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JigsawPuzzleSystemExampleSceneReadMe : MonoBehaviour
{
    /*
    
    Getting started:

    Set all of the puzzle prefabs in the scene to inactive except one.   Click play and it should 
    automatically assign the image (see below) and then scatter the pieces before allowing you to 
    move the pieces around by clicking and dragging them using the mouse.


    Assigning images to the puzzles:

    The "Image assigner" is used to assign the image to the puzzle model.  Try changing the texture 
    to another squarified image with the same original aspect ratio and you'll see that it gets assigned 
    to both the puzzle model material and the ghost image material.  You can use any system you like 
    to do something equivalent but this shows a basic way to do it to give you an idea.


    Notes on the UI buttons:

    The UI buttons are currently hooked up directly to three public methods in the PuzzleManager
    script attched to each puzzle model (via the ButtonClickManager script on the UI canvas).  
    You'll probably want to replace this with an event system that calls these methods indirectly

    The lower left buttons are as follows:
    * An "attract pieces inwards" button - pulls all loose pieces in towards the frame by 50%
    * A "toggle non-edge pieces" button - hides or shows the non-edge pieces
    * A "toggle ghost image" button - hides and shows the ghost image within the puzzle frame

    The upper right has the reset button which will scatter any loose visible pieces while mid-game,
    or re-scatter all the pieces if the puzzle has been completed


    Ghost image:

    The ghost image is a semi-transparent copy of the puzzle image shown within the puzzle frame
    as a guide to help the player.  The alpha of the ghost image material can be hooked up to
    a UI slider so the player can control how strongly the ghost image appears.  


    Saving progress:

    The position of the puzzle pieces is automatically saved by a save system when the scene is closed.
    Take a look at the SaveDataManager on the "Save data manager" game object to see how this is done.
    The name of the puzzle prefab and the name of the image assigned to the puzzle are combined to 
    create a unique name for the puzzle-image combination.

    The save data is reloaded when the scene restarts so if a puzzle is partly solved it will continue
    where the player left off.  If the puzzle has been completed previously the data will be reloaded
    as usual but the puzzle will then immediately perform the puzzle completion sequence, where the
    joins between the pieces are smoothed away and the star particle effect is played.  If you prefer 
    that completed puzzles start again when the scene is restarted then you will need to delete the 
    save data for completed puzzles after the scene is closed.

    In the example scene you can delete the save data for any of the puzzle prefabs by making them 
    active and then clicking the "Delete save data for active puzzles" on the "Save data manager" 
    game object.


    Camera controls:

    The camera controls use two components, one on the camera (CameraManager) and the other on the 
    background (CameraZoomAndPanMouseControls).  These work together to allow the player to zoom in 
    and out using the mouse wheel, and pan around when zoomed in by clicking and dragging on the 
    background.  There are settings on these two components to change the sensitivity of the mouse 
    wheel and which mouse button should be used to perform the dragging.  These should be hooked up 
    to UI controls so the player can adjust the settings to suit their perferences.  Ideally you 
    should also save those preferences and then load them again when starting the scene.


    Puzzle manager:

    The main puzzle manager script is attached to the base object of each puzzle prefab.  The default 
    settings should be good to go but you may want to adjust the scetter exclusion zones to suit your 
    particular UI layout to prevent the puzzle pieces being scattered behind UI elements where the 
    player can't reach them.  There are gizmos to help you visualise the exclusion zones in the scene 
    view.

    If you don't want the puzzle pieces to scatter automatically then uncheck the "Scatter" check box
    under the Scattering settings.  You can then arrange the pieces however you see fit when launching
    the scene.  Note that when the scene is closed the position of the puzzle pieces is automatically
    saved and will be reloaded the next time the scene is started, bypassing the scattering step.

    There are two other scripts that complete the puzzle system: a PuzzlePieceController attached to 
    each puzzle piece, and a ClusterManager that gets created whenever two or more puzzle pieces get
    connected to each other.  You should be able to safely ignore these two scripts.


    Events:

    In the ButtonClickManager script you will notice that it hooks into the static UnityEvents within
    the PuzzleManager so that the UI can respond to events triggered by the PuzzleManager, such as
    the scattering of pieces starting or finishing, or the puzzle getting reset.  Feel free to replace
    these with whatever other event system you prefer, but this should give you an idea of the sort of
    events that are worth knowing about and responding to (e.g. hiding the reset button while the 
    scattering is taking place so the player can't click it again).


    Cheat codes:

    There are two cheat codes hooked up to the puzzle system to allow for easier testimg: pressing 
    "f" will automatically place one puzzle piece closest to the current mouse cursor position; 
    while typing "complete" will automatically complete the puzzle.  These can be found under the 
    "Cheat codes" game object, and you can can see that they each call a method on the 
    ButtonClickManager, which in turn calls the equivalent method on the currently active PuzzleManager. 
    You can edit or disable these cheat codes by altering their settings in the inspector.  I also 
    recommend setting up a global config somewhere with a static property to enable or disable all 
    cheat codes throughout the game, to save you from having to hunt them all down when it comes time 
    to ship a cheat free build of your game.

    */
}
