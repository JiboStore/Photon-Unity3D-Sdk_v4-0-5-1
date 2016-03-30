(c) Exit Games GmbH  - 2014
    developer@exitgames.com
---------------------------

Photon Turnbased Early Access:
https://groups.google.com/a/exitgames.com/forum/#!forum/photon-turnbased-group



Memory Demo
This demo for the "Photon Turnbased" features is a simple Memory game with just a few tiles.
Once two players matched, they take turns at finding to matching tiles. On match, the player
can make another turn. 
With Photon Turnbased, players can leave the game any time and continue later. The state is
saved for each click. If you return to a game, you see the last 2 flipped tiles.
With the default webscripts for Turnbased, we didn't do any work on the server for this demo.


The State
The complete state gets saved per turn (each click that flips a tile). This allows you to 
leave at any point in the game and continue. You can't cheat (easily), cause when you flip
a tile, that gets saved.
The state is turned into a Hashtable for the custom room properties, so keys must be (brief) 
strings. That's why the tile-index is turned into a string, too.
The flipped tiles key has a byte-array with the indexes of visible tiles.
Score is saved per player ID. 


Known Issues
- The state properties are always saved completely (and dumb). 
  If would be enough to save individual tiles that changed!
- No "play again" with the same opponent. 
  This could be added. Then we should count won games, too.
- You only see the last 2 flipped tiles. You might miss some matched tiles of a streak.
- To find a room, someone needs to be in an open one (no matching with inactive players).