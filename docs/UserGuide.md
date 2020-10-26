
# STak User Guide{.center}

## Contents

* [Project Overview][1]
* [WinTak User Guide][2]
* [TakHub User Guide][3]

## Project Overview

STak is an implementation of the [Tak board game][4].  STak stands for "Scott's Tak", but can also be read as a
reference to the stacks of stones used in the game, or as a successor of sorts to [RTak][5], which inspired me to
write my over version of Tak.  STak is comprised of two components:

1. WinTak, a Windows implementation of the Tak game itself.
2. TakHub, a Windows/Linux/MacOS server where players can meet up and play.

WinTak supports both local play against an AI opponent (or another person sitting at the same computer) and remote
play via TakHub, where you can play against either a remote player or an AI running on the hub.  (You can also pit
one AI against another in local AI vs. AI battles, but local AI vs. remote AI games are not currently supported.)

This document describes the mechanics of the WinTak application (e.g., how to make moves, abort a move, undo or
redo a move, ask for a hint, save and load games, rotate the board, zoom in and out, modify configuration settings,
and so on).  TakHub is discussed as well, although to a much lesser degree (as there aren't a lot of features to
describe).  Details on the status of the project, including known bugs and possible enhancements, can be found in
the [Project Status][6] document.

The game rules themselves can be found elsewhere online:

* [Game rules document][7]
* [Game rules video walkthrough][8]

If you enjoy playing Tak, support the creators by purchasing the physical board game from [Cheapass Games][9]!

## WinTak User Guide

### Changing the View

The main window displays a 3D view of the game board.  While the camera always remains directed at the center of the
board, the camera can be zoomed in and out and the board can be rotated using the mouse and keyboard, so that the
board can be seem from different distances and angles.

- To zoom in and out you can either use the mouse wheel (if the mouse is so equipped) or the keyboard shortcuts Ctrl-+
  (to zoom in) and Ctrl-- (to zoom out).

- To resize the font size of the "Player One vs. Player Two" banner text, hold down either shift key while rotating
  the mouse wheel.  This will resize the banner text rather than zoom the view of the table (board and stones).
  You can also change change the font used in the banner text by holding the control key down while spinning the
  mouse wheel.  You can specify in a configuration file the list of fonts to be used in the banner text.

- To rotate the board to an arbitrary orientation in 3D space, click and hold the left mouse button while the mouse
  cursor is anywhere in the game window (other than an active area such as over a stone that can be legally picked up),
  then drag the mouse in the direction of the desired rotation.  To rotate the board around its vertical axis (that is,
  to rotate it while keeping it "flat on the table") use  the left and right arrow keys.  You can speed up this "flat"
  rotation by a factor of five by holding down the Ctrl key while also holding down the left or right arrow key, but
  the Ctrl key must be pressed prior to pressing an arrow key, otherwise pressing the Ctrl key will halt the rotation.

- To reset the view to its default zoom level and orientation, use the Reset View menu item in the View menu or the
  Ctrl-R shortcut.

If you hold down the shift key while moving the mouse over the board, a the name (file and rank) of the cell pointed
to by the mouse cursor is displayed in a small overlay.  When the cursor points at a stone, the overlay indicates the
stone and model IDs; these are internal game object IDs used purely for debugging purposes.

### Basic Game Mechanics

When WinTak is first started you are immediately placed in a game as Player One on a 5x5 board, playing against an AI
opponent, so you can make your first move forthwith.  By default the table is rotated so that Player Two's stone
reserve is in front of you.  After both players have made their first move it is then rotated 180 degress, so that
your stones are directly in front of you.  (This behavior can be changed through a setting in the configuration file;
see [Configuration Settings][11] below.) The initial rotation of the table ensures that you don't need to reach
over the table to grab one of your opponent's stones on your first move. :-)

The game of Tak allows two types of moves, which I'll refer to here as Stone moves and Stack moves.  A Stone move
is a move in which you draw a stone from your reserve and place it on an empty cell on the board.  A Stack move is
a move in which you grab one or more stones from the top of a stack on the board, and move along a straight line
dropping one or more stones from the bottom of the grabbed stack on each cell along the way.

To make a Stone move, position the mouse cursor over a stone in the appropriate reserve (i.e., a flat stone from your
opponent's reserve on your first move, and any accessible stone from your reserve on subsequent moves), then click
the left mouse button to grab the stone.  If the stone is a flat stone, clicking the left mouse button again while
holding the stone will rotate it so that it becomes a standing stone; another click will return it to its flat
position.  Move the stone to the empty board cell on which you wish to drop the stone.  When the stone is positioned
roughly over a cell on which it can be dropped it will become highlighted, indicating that it may be placed on that
cell.  Moving the stone further toward the center of that cell will cause it to snap to the center of the cell.
Clicking the left mouse button any time the stone is highlighted will place the stone on the cell over which it
hovers; it is not necessary to snap/center the stone before dropping it.  Dropping the highlighted stone completes
your turn.  (Note: the sizes of the areas covered by the highlighting and snapping zones are configurable.)

When changing the orientation of a stone between flat and standing be sure that the stone is not highlighted,
indicating it may be dropped at that location, otherwise the click of the mouse button will be taken as an
indication to drop the stone.

To make a Stack move, move the mouse so that the cursor is over the bottommost stone in the stack of stones you
want to grab, then click the left mouse button to grab the stones.  Move to the adjacent cell in the desired direction
and click once for each stone you wish to drop on the cell.  If you're still carry stones after dropping stones,
move to the next cell and repeat.  Your turn ends when you've dropped the last stone of the stack you grabbed.

By default, when the mouse cursor moves over a stone that can be legally picked up to initiate a move, whether
in a reserve or on the board, that stone, along with any other stones that would be involved in the move (i.e.,
the stones above that stone in the stack being picked up) become highlighted.  The highlight is removed when the
cursor moves away from the stone or when the stone/stack is picked up.  Stones being carried are unhighlighted,
but become highlighted again when they are centered over a cell in which they can be legally placed; the highlight
is removed from a stone once the stone has been dropped into place.  The manner in which stones are highlighted
(or whether they are highlighted at all) can be changed through settings in the configuration file.

Both Stone and Stack moves can be canceled/aborted at any point before the move is complete by clicking the right
mouse button.


### Undoing and Redoing Moves

Under certain conditions, the slider bar at the top left of the play area can be used to undo and redo moves.  When
enabled, dragging the slider thumb to the left will undo moves; dragging it to the right will redo moves at the same
rate.  Alternatively, clicking on the slider bar to either side of the thumb will jump directly to that move, undoing
or redoing all necessary moves.  Pressing and holding the left mouse button on the thumb of the slider will display a
small tooltip showing the number of the move that ended at that position.

In addition to using the slider, moves can be undone and redone using the Edit=>Undo and Edit=>Redo menu items or
their keyboard shortcuts (Ctrl-Z and Ctrl-Y).  Unlike when using the slider to undo/redo moves, moves that are
undone/redone using the menu items or associated shortcuts are animated according to the current move animation
rate (see [Move Animation][12] below).  This allows you to watch as each individual move is undone or redone.

Move undo/redo is supported in three scenarios:

1. You are involved in a local game against another player (i.e., you and your opponent are sitting at the same
   computer, running a single instance of WinTak) and the game is either active or completed.  Undoing or redoing
   a move in this case undoes/redoes individual moves one by one.

2. You are involved in an active or completed game against an AI opponent, either local (WinTak AI) or remote
   (TakHub AI).  Undoing or redoing a move in this case undoes/redoes two moves at a time, because the first undo
   undoes the AI's most recent move, and unless another move is undone/redone the AI would instantaneously make a
   new move.

3. You are involved in a completed game against a remote human player.  Undo and redo are allowed, but can only be
   done by the player whose move is being undone or redone.  Undo and redo are *not* currently allowed in active
   (not yet completed) games against a remote human player.


### Forcing AI Move Decisions

When it's an AI's turn to play, the AI "thinks" by performing a search for what it thinks is the best move.  The AI's
search algorithm is implemented using a standard minimax search tree with alpha-beta pruning, using a static evaluator
to evalute possible moves.  By default WinTak searches the tree of possible moves to a depth of three, but this can be
changed via a configuration file setting.  Note that increasing this value can increase AI thinking time considerably,
and won't always (or maybe even often) result in moves that are significantly "smarter."

If an AI is taking an exhorbitant amount of time to decide on a move, you can cut short its thinking and have it make
the best move it's found so far.  To do so, type Ctrl-C while the focus is somewhere in the main window.  There are
no menu items or other mechanisms other than this keyboard shortcut to force an AI to halt its thinking.  Additionally,
this works only when playing against a local AI; thinking hub-based AIs can't yet be interrupted.


### Move Animation

The slider bar at the top right of the main window can be used to adjust the move animation speed.  Sliding it all
the way to the right will disable animations altogether, so the stones involved in a move will simply disappear from
their starting positions and instantly appear at their final locations.  The move animation rate affects moves made
by AI players as well as moves that are undone or redone (and hints, described next).


### Hints

When playing against a WinTak AI opponent you can ask for a hint whenever it is your turn to make a move.  When a
hint is requested WinTak recommends a move by animating that move, then restoring the affected stones to their
original positions.  Hints are not supported in human vs. human games, or in games against an AI running remotely
in a TakHub server.  Note that if you request a hint from the same position repeatedly, the AI may suggest different
moves each time the hint is requested.  This is especially true when there's no clear "best" choice for the move.


### Saving and Loading Games

You can save an active or completed game at any time using the File=>Save or File=Save As... menu items.  Similarly,
you can open a saved game using the File=>Open... menu item.  When you open a saved game, the game is replayed from
the beginning.  (You can change the speed at which it is replayed using the animation speed slider.)  The players
are set up to both be local human players, with names as indicated in the saved game file; as a result, undo/redo
are available after the game has finished loading.

In addition to loading and saving games using the Open/Save menu items, you can also use the Copy PTN and Paste PTN
menu items in the Edit menu.  The Copy PTN command copies the text of the PTN representing the game to the clipboard;
the Paste PTN command treats the contents of the clipboard as PTN text and loads the game described by that text in
the same manner as would be done if you instead used the Open... command to open a file containing that text.  Note
that the Paste PTN menu item is only enabled when the clipboard contains valid PTN.  The usual shortcut characters
for copy and paste, Ctrl-C and Ctrl-V, can be used as alternatives to the menu items.


### Artificial Intelligences

WinTak is designed to support (to a degree) AI plugins; that is, AI's built independently from WinTak itself and
loaded dynamically at runtime.  Currently there are two AIs, named "Dinkum Thinkum" and "The Experiment".  The former
is the default AI, which is built into WinTak itself.  The latter is composed of C# source code that is dynamically
loaded and compiled each time WinTak is started.  (The code itself is in ExperimentalTakAI.cs in the plugins folder.)
This allows me (or you!) to tweak the algorithm to experiment with different weightings assigned to different board
states.  Without access to a more complete API for working with the game there's not a lot you can do here other than
change the various weightings used to compute a score, but it's a start.  If you're familiar with the C# language and
want to experiment with this AI you're free to do so, but I strongly suggest that you make a backup of the source file
before modifying it.

Dinkum Thinkum is what I would call moderately intelligent, by which I mean not particularly smart, but not completely
idiotic either (usually - it sometimes makes remarkably stupid decisions).  Currently, The Experiment uses the same
algorithm as Dinkum Thinkum and thus behaves identically to it.


### Playing on TakHub

**Note:** Yes, I know.  The TakHub window and associated dialogs are ugly, and should be redesigned or at least
prettified somehow.  I agree.  They will.  But for now, they're ugly.

You can play against remote players or AIs by connecting to a TakHub server using the "Connect to Hub..." item in the
TakHub menu.  Selecting this menu item will bring up a login/register dialog.  If you don't already have an account
on the server you can create one by checking the "Register as new user" checkbox, entering your chosen user name and
password (in both password fields) and email address (currently unused - you can enter anything that looks like an
email address, such as "foo@bar.com"), and clicking the OK button.  If you already have an account on the server you
can simply sign in with your username and password.  In either case, you'll need to set the hostname/IP and port
fields to the address and port of the TakHub server you want to connect to.

Once connected a TakHub window should appear, listing all active games and outstanding invitations.  By default the
server hosts five AIs of each of the two AI types, so if no one else is connected to the server you won't see any
active games, but you'll see ten invitations from the AIs.  You can double-click one of these to play an AI game.
If other players are present and have outstanding invitations you can instead double-click on one of those, or you
can create an invitation yourself that others may accept.

**Note:** When playing a (human) opponent on TakHub, mouse tracking messages are sent from WinTak to TakHub whenever
you are in the process of making a move; these are immediately forwarded to the opponent's copy of WinTak so that it
can update its view of the board appropriately.  The rate at which these mouse tracking messages are sent is defined
by the notificationInterval configuration setting.  Correspondingly, your opponent's configuration setting determines
the rate at which she will be sending tracking messages to you, and you have no control over this rate.  Thus, neither
you nor your opponent have full control over the mouse tracking notification rate.

The optimum value for the notificationInterval setting depends on the latency of both your connection to TakHub, and
your opponent's connection, and is best determined through experimentation.  Generally speaking, on a low-latency
connection the notificationInterval value should be set to a value somewhat smaller than the default value (currently
60 milliseconds), while a higher value is more suitable for higher latency connections.


### Execution Log Windows

There are a few "execution log" windows that can be opened independently of the main window.  The first displays the
game moves in PTN format; this window is updated whenever a move is made, undone, or redone.  The second displays
some bitboards that are used by the AI; these are purely for debugging purposes.  The third window, like the second,
is purely for debugging purposes; it contains the log messages written by the application.  By default this debug
logging is disabled; it can be enabled by typing one of the digits 1-5 while the main view has the focus.  There is
no feedback for this (other than the change to the number of messages that appear in the debug window).  Higher digits
produce more log messages.  To get full logging, type either '4' or '5'.  A value of '1' will log only errors, and
'0' will disable logging altogether.


### Configuration Settings

WinTak has various settings that can be specified in its configuration files:

* uiappsettings.json      - User interface settings
* interopappsettings.json - WinTak <=> TakHub interop settings

The interop settings relate to WinTak and TakHub interoperation, and don't generally need to be changed from their
default values.  The uiappsettings.json file is where most of the "interesting" settings reside.  These all relate
in some way to user interface appearance or behavior.  This is where you'll want to spend your time tweaking things,
if you indeed want to tweak things.  The settings are documented, to a degree, in the configuration files themselves.

Eventually I plan to allow many of the available settings to be configured via a suitable user interface, but for now
changing configuration settings requires you to edit these text files.  If you do decide to make changes to one of
these files, make sure to create a backup copy of the file before you do.  You can edit settings in uiappsettings.json
from within WinTak by accessing the "Advanced Options..." item in the Options menu.  Selecting this menu item will
open uiappsettings.json in notepad.exe.  (If the EDITOR environment variable is set, its value will be used as the
editor be used in preference to notepad.)  After you've made your edits to the file and quit notepad WinTak will
check to make sure the file contains valid JSON; if an error is found you can choose to re-edit the file to fix the
problem, or to discard the changes and revert to the original version of the file.  Note, however, that if you set
configuration values that are syntactically valid but otherwise unacceptable (e.g., of the wrong type) WinTak will
accept the changes, but may crash later when errant configuration settings are next accessed by the game.

For the most part, changes to configuration settings are recognized at the start of the next turn of the game.
However, there are a few settings for which changes are not immediately recognized; the application must be restarted
for these to take effect.  If you change a setting and don't see the change reflected after the next turn has started,
try restarting the application.

If you're going to change configuration file settings, please note the following:

* **WinTak currently does minimal error checking when loading these files.**  If a file is not a properly formatted
  JSON file, or the value of a setting has the wrong type (e.g., integer rather than string) or has an unsupported
  value, or if a setting is removed from the configuration altogether, WinTak is likely to crash.  **Make a backup
  up these files before modifying them!**

* It's possible that a few rarely used settings do not work as documented, as there are some that have not been
  tested significantly.

Beyond these configuration settings there are a few user preferences that can be changed from within WinTak itself;
these relate to the appearance of the board and stones, and can be accessed using the Options=>Appearance... menu
item.  You can also enable sound using the "Enable Audio" toggle in the same menu.  (There are currently only a few
sound effects, which are played when stones are dropped and when you finish a game.  Unfortunately, the sound player
provided by the framework seems to have some latency issues, so sounds are sometimes delayed or clipped, and sometimes
they're not played at all.)


## TakHub User Guide

[This section is an early draft and will be refined later.]

TakHub is a cross-platform (Windows/Linux/MacOS) web application server written in C# using the ASP.NET Core 3.1
framework.  It serves as a hub where WinTak players can meet and play with one another, or against AIs running on
the hub.  TakHub is a console application; it has no user interface, browser-based or otherwise.  In order to modify
the way it runs, you must modify a configuration file.

### Limitations

* TakHub does not support secure connections using HTTPS; only HTTP is supported.

  When creating accounts, users should not reuse any password they use in any of their "real" accounts.  Credentials
  will be sent to the server in plain text, and could be seen by a malicious actor while in transit.


### Configuration

TakHub uses a number of configuration files (all in JSON format, with a ".json" extension) and all residing in the
main TakHub directory.  However, the only file you'll need to modify is appsettings.json, and there are only a few
settings you'll want to modify.  In particular, any users planning to connect to the server will need to know the
address(es) and port(s) you've configured TakHub to listen on, so you may want to modify the associated setting.

**Note**: If you're running TakHub on a home computer situated behind a router you'll need to forward traffic destined
for the port you're using from the router to your computer.  How this is accomplished varies from router to router,
so you'll need to glean this information from the documentation for the router you're using, or by poking around the
router's web interface.  It's normally in a section named "Port forwarding" or something similar.

Following are the only settings you should modify.  Changing any setting not listed here voids the warranty.

* urls

    This setting is used to specify the internet addresses TakHub will listen on for connections.  The default is
    "http://*:2735", which means that TakHub will listen on port 2735 for connections on all of the computer's
    network interfaces (including the loopback address).  You can specify a specific address by replacing the
    asterisk with an IP address (IPv4 or IPv6) or hostname, and you can specify multiple addresses separated by
    semicolons.  The default value for this setting should be appropriate for most users, so it's not strictly
    necessary to modify it.

* allowedHosts

    This setting specifies the addresses from which connections will be accepted.  The default is "\*", which means
    connections from any IP address are allowed.  You can specify one or more addresses separated by semicolons.
    So for example if you just wanted a particular friend (and yourself) to be able to connect to your server, and
    your friend's computer (or router if they're on a home network) has IP address 136.52.23.18, you could specify
    "136.52.23.18;localhost" as the allowedHosts value.  You could then run WinTak on the same computer as you're
    running TakHub and make a connection via the localhost address, while your friend connected from his computer.
    Like the urls setting, this setting can be left with its default value of "\*" unless you for some reason need
    to restrict who can access the server.

* aiBehavior

    The settings in this section affect the AIs in various ways; they're identical to WinTak's settings of the same
    name.  Other than treeEvaluationDepth you should leave the settings as they are.  Changing the treeEvaluationDepth
    for an AI will change how deeply that AI searches for each move.  Increasing this value *might* improve the quality
    of moves an AI makes, but it will certainly have adverse affects on its thinking time.  Be wary of raising this
    beyond a value of four.

* logging

    The logging section has a subsection called logLevel, which in turn has a setting named "default".  Changing
    the value assigned to "default" changes the logging level to that value, which in turn affects how much
    information TakHub writes to the console.  Acceptable values are Off, Error, Warning, Information, and Debug.


### Running the Server

To run TakHub, double-click on TakHub.exe in Windows Explorer or run it from within a console window (PowerShell,
Cmd, etc.).  If you run it by double-clicking, it will create a new console window, otherwise it will write to the
console you ran it from.

The first time TakHub is run it will create a database in which to store user account information, so it may take a
minute or more to finish initializing.  On subsequent executions TakHub should start up much faster.  During
initialization TakHub will write some messages to the console window, ending with something like this:

    Now listening on: http://[::]:2735  
    Application started. Press Ctrl+C to shut down.

**Note**: Although the message indicates that typing Ctrl-C into the console will shut the server down, it does not
always do so.  If it doesn't you can kill it by running "Get-Process TakHub | Stop-Process" in a PowerShell console
window, or by closing the console window itself.

At this point the server is ready to accept connections.  To verify that TakHub is working properly you can take the
following steps, assuming that you've installed TakHub on a Windows computer on which WinTak is also present.


1. Start WinTak running and select "Connect to Hub..." from the TakHub menu.  This will open the TakHub connection
   dialog.  In this dialog specify "127.0.0.1" or "localhost" as the server's address.  If you've configured TakHub
   to listen on a different port than the default, modify the port number in the dialog to match.  Decide on the
   username you want to use when playing on the hub, and enter it along with a password of your choice (a minimum
   of eight characters). Then check the "Register as new user" checkbox and retype the password in the newly
   visible password field.  Finally, specify an email address and then click the OK button.  (The email address is
   not currently used; you can specify anything that looks like a valid email address, such as "foo@bar.com".)

   **Note:** Don't reuse a password that you're using for a "real" account of some sort.  The username and password
   will be sent in plain text to the server and could be intercepted.  (This will be resolved when support for HTTPS
   has been added to TakHub.)

2. After a few seconds (or perhaps several seconds) WinTak should display a window showing the active games (none)
   at the top, and the outstanding invitations below them.  By default TakHub hosts five of each of the two AIs,
   so you should see these listed as open invitations.

3. Double-click one of the invitations to open the invite acceptance dialog.  Choose the desired table size and player
   position and click OK.  The main WinTak window should display the appropriate board and be ready to begin play.
   If the AI is playing as Player 1 it will immediately make its first move.  Go ahead and make a few moves to verify
   that the game behaves properly.

4. Open another instance of WinTak and connect this one to TakHub as well.  For testing purposes, create a second
   account to use; don't use the same account for both connections.  Once connected, invite a new game by pressing
   the "Invite new game" button.  In the dialog that appears, select the board sizes and player positions you're
   willing to play, and ensure that the "Will play against AI opponent" checkbox is *not* checked.  Then click OK.

5. Move back to the first WinTak instance and double-click the new invitation, which should appear at the top of
   the list of outstanding invites, above those of the AIs.  Select the board size and player position you prefer,
   then click OK.  If you're in the middle of an active game you'll be asked whether you want to quit.  Click the
   Yes button.  The new game between the two connected players should now begin.

6. If things don't work, send email to tak@smokerboy.com and I'll see what I can do to get things working.


### The TakHub Database

By default TakHub uses SQLite as the database provider.  SQLite stores the database in a single file named TakHub.db in
a subdirectory of the TakHub directory named "Database."  To store the database elsewhere, you can modify the "sqlite"
configuration setting in appsettings.json, replacing "TakHub.db" with the full pathname of the file to use (leaving
the rest of the sqlite connection string as it is).

In addition to SQLite, which is supported on all platforms, SQL Server is supported on Windows (only).  To use SQL
Server rather than SQLite, change the value of the "databaseProvider" setting in appsettings.json from "sqlite" to
"sqlserver".  (You don't need to modify the connection string stored in the sqlServer connection string setting.)
Note that SQL Server stores the database in the home directory of whatever user runs TakHub, rather than in TakHub's
Database subdirectory.

The TakHub database stores user accounts (username, password hash/salt, email address) as well as some transient
data used internally.  Over time I suspect the database will be used to hold additional data such as login history,
a list of games played by each player (and the results of those games), ranking data, etc.  However, for now only
account credentials and email addresses are stored (TakHub does not yet make use of the email addresses).

If for some reason you want to remove all user accounts and start afresh, simply stop the TakHub server if it's
running, then delete the database file(s).  TakHub will create a new empty database the next time it is run.


## Comments, suggestions, questions and bug reports are welcome!

If you have comments, questions, or suggestions for new features I'd love to hear them.  I already have a number of
enhancements in mind (see [Project Status][6]), but I'm sure there are lots of great ideas that I haven't considered.
Additionally, if you encounter any bugs I'd love to hear about them, so bug reports are welcome as well.  I can be
reached by email at <tak@smokerboy.com>.

[1]:  <#project-overview>                                                                             "Project Overview"
[2]:  <#wintak-user-guide>                                                                            "WinTak Documentation"
[3]:  <#takhub-user-guide>                                                                            "TakHub Documentation"
[4]:  <https://www.wikiwand.com/en/Tak_(game)>                                                       "Tak on Wikipedia"
[5]:  <https://www.reddit.com/r/Tak/comments/3o8fcw/windows_tak_game_with_ai_source_code_available/> "RTak"
[6]:  <ProjectStatus.html>                                                                           "Project Status"
[7]:  <https://cheapass.com/wp-content/uploads/2018/04/UniversityRulesSM.pdf>                        "Game Rules"
[8]:  <https://youtu.be/iEXkpS-Q9dI>                                                                 "Tak Walkthrough"
[9]:  <https://cheapass.com/>                                                                        "Cheapass Games"
[10]: <https://cheapass.com/tak/>                                                                    "Classic Edition"
[11]: <#configuration-settings>                                                                      "Configuration Settings"
[12]: <#move-animation>                                                                              "Move Animation"

