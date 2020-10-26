
# STak Project Status{.center}

## Overview

STak is a work in progress; I plan to add features and fix bugs as time permits.  At present (October 2020) WinTak is
fully functional, in the sense that is supports play against both local AI players and remote players, via TakHub.
It supports most of the basic features one would expect in a Tak game, a notable exception being support for timed
games.  The UI is usable but has some rather ugly corners at the moment.

TakHub has most core functionality implemented; it can host games between remote players or one remote player and an
AI running on the hub, is supports kibitzing, has basic text chat, etc.  Authentication is rudimentary and not fully
implemented (e.g., JWT refresh tokens are not yet working, there's no password reset functionality, etc.).  TakHub
has not been well tested.

### WinTak Status

Basic functionality is working.  No catastrophic (known) bugs; however, some stone rendering issues have been
witnessed during play against a remote player over the internet.  WinTak currently supports these features:

  * Play games against local AIs.
  * Play games against remote (TakHub-resident) AIs.
  * Play games against remote (TakHub-connected) players.
  * Infinite (within a single game) undo/redo.
  * View control: rotate, zoom, reset to default.
  * Move/Abort/Undo/Redo move animation.
  * Control over move animation speed.
  * Animated move hints in games against local AIs.
  * Personalization/configuration (primarily via configuration file for now).
  * Save and load games (PTN format).
  * Basic sound effects.
  * Chat window for text-based chat with other TakHub users.
  * Pluggable AIs - Load and compile C#/.NET-based AIs at startup.

### TakHub Status

Basic functionality is working.  Not at all well tested though, and I'm sure it's far from bug-free.  Consider it
a pre-alpha release.

  * Host games between human players.
  * Host games between humans and AIs running on the hub.
  * Match game invitations according to game spec (board size, player number, specific players).
  * Users can kibitz (join a table to watch the game and chat with others at the table).
  * Users can chat to specfic (named users).
  * Allow game invitations to disallow kibitzing.


## WinTak Limitations / Issues / Possible Enhancements

#### Catastrophic Issues

A catastrophic issue is one that crashes the application or otherwise renders it unusable without a restart.

* [There are currently no known catastrophic issues.]

#### Major Issues

A major issue is one that doesn't result in an application crash, but otherwise interferes in a significant way with
the playability of the game.  This includes, for example, a bug that causes the game in progress to misbehave in a
such a way that the game must be abandoned and a new game started, but does not require the application itself to
be restarted.

  * When playing against a remote player via TakHub, rendering problems can occur.  In particular, I've twice seen a
    stone rendered below its proper location, with the top of the stone appearing to be at the same level as the top
    of the board.

  * Errors (exceptions) are sometimes raised when attempting to kibitz a game while a move is in progress, or at
    other times while kibitzing.

#### Minor Issues

A minor issue is one that isn't major or severe.  Generally this means that minor issues may be irritating but are
not so bad that they disrupt or otherwise interfere with gameplay.

  * Asking for a hint after undoing moves effectively truncates those undone moves, so they can't be redone.

  * The audio clip that is played when a stone is dropped is sometimes delayed (high latency), or clipped, or not
    played at all.

  * After completing a game against an AI, the undo/redo move slider cannot be used to undo moves.  However, using
    the Undo Move or Ctrl-Z shortcut to undo a single move resolves the issue, and the slider can then be used.

  * Various glitches related to entering and leaving hub-based games.


### Potential Enhancements

#### Make it pretty:

  * Display information about the active game (top center of main view, probably), including player names and types
    (i.e., whether they are local or remote, human or AI), the current turn (number and which player is active),
    and the score upon game completion.  (It would also be nice to make this information interactive, e.g., to allow
    players and the board size to be selected directly in the main view, obviating the need for the current New Gamed
    dialog.)

  * Redesign the TakHub window and Invite New Game dialog; they're ugly and uninviting.

  * Make consistent the padding, margins, borders, etc. on each dialog.  Style everything nicely.

  * Learn how to use Blender (or something similar) and create nicer board and stone models.

  * Find nicer images to use for the board and stones.

  * Find better sound clips to use for stone drops and winning/losing.

  * Improve stone movement, especially with regard to moves made by human players rather than AIs.  I'm not yet sure
    how to go about improving this, though.

  * Improve stone highlighting somehow.

  * Add more flexibility to the lighting configuration.

#### Make It Do More Stuff:

  * Implement timed games.

  * Implement alternative scoring variants (Downings, Middletown, Tarway rules).

  * Allow a hub-based player to request permission to undo the move they just made, before their opponent has made
    their own move.  The opponent of the player making the request would have the option to either allow or deny the
    request.

  * After loading a saved game, provide a means of setting either or both of the players as an AI.  This would allow
    the player to set the current position (turn) of the game, then convert the player they wish to compete against
    to an AI (for example, try different endplay strategies).

  * Allow AI vs. AI games to be paused.

  * Provide an option to record (save as PTN files) some number of the most recently played games automatically.

  * Determine after each turn whether a win ("Tak") is possible on the next move, and (optionally) indicate it.  That
    is, have the AI announce Tak appropriately.  (Hub players have a chat window to use to communicate, but a more
    specific mechanism such as a Tak button in the main view would be nice.)

  * Support the MessagePack and System.Text.Json protocols for WinTak <=> TakHub communications.

  * Support PTN files containing multiple turns per line.  Very low priority.  Does anyone write such PTN files?


## TakHub Limitations / Issues / Possible Enhancements

### Limitations

* It does not support secure connections using HTTPS; only HTTP is supported.

  When creating accounts, users should not reuse any password they use in any of their "real" accounts.  Credentials
  will be sent to the server in plain text, and could be seen by a malicious actor while in transit.


### Catastrophic Issues

A catastrophic issue is one that crashes the application or otherwise renders it unusable without a restart.

* [There are currently no known catastrophic issues, but I'm sure they must exist so plan to encounter them.]

### Major Issues

A major issue is one that doesn't result in an application crash, but otherwise interferes in a significant way with
the playability of the game.  This includes, for example, a bug that causes a player to need to disconnect from and
then reconnect to TakHub, but does not require the server itself to be restarted.

* Errors can occur when kibitzing a hub game, causing the game view to get out of sync and generally requiring that
  the player kibitz the game again.

* [Other bugs will be added as they are found (and many undoubtedly will be) during testing.]

### Minor Issues

A minor issue is one that isn't major or severe.  Generally this means that minor issues may be irritating but are
not so bad that they disrupt or otherwise interfere with gameplay.

* [This section will be filled out as bugs are found during testing.]

### Potential Enhancements

  * [This section will be filled out when time permits.]

