# STak

STak (Scott's Tak) is an implementation of the game of Tak, a two-player strategy game first introduced by Patrick
Rothfuss in his novel "The Wise Man's Fear" and later developed into a real-world game by James Ernest of Cheapass
Games ([see here](https://cheapass.com/tak/)).

STak is comprised of two primary applications:

1. A standalone Tak game for Windows named WinTak.  WinTak supports local play against an AI opponent (or against
another player sitting at the same computer), and against a remote player or AI opponent by connecting to a TakHub
server.

2. An ASP.NET Core-based server named TakHub, which serves as a hub where players can meet up to play against one
another or against AIs running on the TakHub server.  TakHub also supports kibitzing - watching a game being played
by two remote players or by a remote player and a TakHub-based AI.  TakHub is a cross platform application, running
on Windows, MacOS, and Linux.

Both WinTak and TakHub are fully functional; however, development is ongoing and additional features are planned for
both applications.  WinTak is currently fairly stable; crashes and bugs affecting the play of the game are very rare
in player vs. local AI scenarios.  Remote play via TakHub is somewhat less stable, with issues sometimes arising when
entering or leaving a game, but gameplay itself generally proceeds smoothly.

## WinTak Features

WinTak features include:

  * Play games against local AIs.
  * Play games against remote (TakHub-resident) AIs.
  * Play games against remote (TakHub-connected) players.
  * Infinite (within a single game) undo/redo.
  * 3D view of the game board and stones (those in play as well as unplayed stones).
  * View control: rotate board/stones in 3D, zoom in/out, reset view to default.
  * Move/Abort/Undo/Redo move animation.
  * Slider control for adjusting the move animation speed.
  * Animated move hints in games against local AIs.
  * Personalization/configuration (primarily via configuration file for now).
  * Save and load games in PTN (Portable Tak Notation) format.
  * Basic sound effects.
  * Chat window for text-based chat with other TakHub users.
  * Stop an AI's thinking (it will choose the best move it's found so far).
  * Very fast AI move enumeration and evaluation implementation.
  * Pluggable AIs - Load and compile your own C#/.NET-based AIs at startup.

## TakHub Features

TakHub features include:

  * Host games between human players.
  * Host games between humans and AIs running on the hub.
  * Match game invitations according to game spec (board size, player number, specific players).
  * Users can chat to specfic (named) users or to everyone sitting at the user's game table.
  * Users can kibitz (join a table to watch the game, and to chat with others at the table).

## Documentation

WinTak has a fairly comprehenive User Guide, which can be accessed from the Help menu.  The User Guide includes
some basic information on how to get TakHub up and running as well.

## Build

Building the code requires that the .NET 5 SDK be installed.  Run "dotnet build" in the Tak\src directory to build
the complete solution, including both WinTak and TakHub.

## Running WinTak and TakHub

The build process copies all necessary resource and configuration files to the WinTak and TakHub output directories
along with the application binaries and associated DLLs, so both WinTak.exe and TakHub.exe can be run directly from
their respective build output directories.

## Installation

No installer has been implemented for either WinTak or TakHub, nor is one currently planned.

## License

All WinTak and TakHub code is covered by the MIT license, which can be found in the [LICENSE](LICENSE) file.
All WinTak code, as well as all TakHub code related to the Tak game itself, was written by and is copyrighted
by Scott Southard (scott.southard@gmail.com).  Much of the TakHub code dealing with user authentication and JWT
(JSON Web Token) hanlding was written (and is copyrighted by) Mark Macneil (mark@fullstackmark.com).

## Contact

tak@smokerboy.com
