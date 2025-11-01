# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Odebuchan** is a Unity 6000-based multiplayer game built with Photon Fusion 2.0.7 for real-time networking. The project targets WebGL as its primary build platform and uses Unity's Input System for player controls.

- **Product Name**: Odebuchan
- **Unity Version**: 6000.x (Unity 6)
- **Networking**: Photon Fusion 2.0.7 (Shared Mode)
- **Target Platform**: WebGL
- **Player Count**: 2 players (1v1 matchmaking)

## Unity Development with MCP

This project uses the Unity MCP server (com.coplaydev.unity-mcp) to enable Claude Code to interact directly with the Unity Editor. You can:

- **Manage GameObjects**: Use `mcp__UnityMCP__manage_gameobject` to create, modify, or find objects in scenes
- **Edit Scripts**: Use `mcp__UnityMCP__script_apply_edits` for structured C# edits or `mcp__UnityMCP__apply_text_edits` for precise line/column edits
- **Scene Management**: Use `mcp__UnityMCP__manage_scene` to load, save, or query scene hierarchy
- **Editor Control**: Use `mcp__UnityMCP__manage_editor` to play/pause/stop the editor, check compilation status
- **Console Logs**: Use `mcp__UnityMCP__read_console` to read Unity console messages for debugging

Always validate scripts after editing using `mcp__UnityMCP__validate_script` to catch compilation errors early.

## Build Configuration

The project is configured for WebGL builds with the following scenes in build order:
1. TitleScene (index 0) - Main menu and matchmaking
2. GameScene (index 1) - Main gameplay scene

**Build Settings Location**: `ProjectSettings/EditorBuildSettings.asset`

WebGL-specific scripting defines are automatically set:
- `FUSION_WEAVER`
- `FUSION2`, `FUSION_2`, `FUSION_2_0`, `FUSION_2_0_7`
- `FUSION_2_OR_NEWER`, `FUSION_2_0_OR_NEWER`

## Networking Architecture

### Photon Fusion Shared Mode

The game uses Photon Fusion's **Shared Mode** (formerly called "Client/Server" in Fusion 1.x), where one client acts as the session host/master.

**Key Components**:

1. **NetworkRunnerHandler** (`Assets/Scripts/Title/NetworkRunnerHandler.cs`)
   - Implements `INetworkRunnerCallbacks`
   - Manages session lifecycle: connection, player join/leave, scene transitions
   - Starts game with `StartGame(GameMode.Shared, sessionName)`
   - When 2 players join, triggers scene transition to GameScene with 1-second delay for WebGL connection stability

2. **NetworkPlayer** (`Assets/Scripts/Network/NetworkPlayer.cs`)
   - Spawned for each player in the session
   - Manages player name via `[Networked] NetworkString<_16> PlayerName`
   - Stores player name from `PlayerPrefs` (key: `TitleScreenManager.PLAYER_NAME_KEY`)
   - On spawn, tells GameManager (server-side) to create UyopyonState for this player

3. **UyopyonState** (`Assets/Scripts/Data/UyopyonState.cs`)
   - Represents each player's character/avatar state
   - Networked properties: `Weight`, `Energy`, `CurrentStatus`, `OwnerPlayer`
   - Uses `Render()` method to detect changes and update UI on all clients
   - Spawned by GameManager with specific player's `InputAuthority`

### Network Object Spawning Flow

```
1. Player connects → OnPlayerJoined() called
2. Host spawns NetworkPlayer prefab with player's InputAuthority
3. NetworkPlayer.Spawned() executes on all clients
4. If server: GameManager.SpawnUyopyon() creates UyopyonState for that player
5. UyopyonState.Spawned() registers with UIController for display
```

### Important Patterns

- **Authority**: NetworkPlayer and UyopyonState use `Object.InputAuthority` to determine which player controls them
- **Server-side Logic**: GameManager runs game logic only on host (`Runner.IsServer` or `Runner.IsSharedModeMasterClient`)
- **Client-side Rendering**: UyopyonState and NetworkPlayer use `Render()` to poll for changes and update UI locally (not OnChanged callbacks)
- **Dictionary Tracking**: GameManager maintains `Dictionary<PlayerRef, UyopyonState>` to map players to their state objects

## Code Organization

```
Assets/Scripts/
├── Data/           # Networked data structures
│   └── UyopyonState.cs
├── Game/           # Core game logic (host-authoritative)
│   ├── GameManager.cs        # Singleton, manages game state & turn flow
│   ├── TurnProcessor.cs      # (Stub) Turn-based game logic
│   └── SpecialAbility.cs
├── Network/        # Networking layer
│   └── NetworkPlayer.cs      # Per-player network object
├── Title/          # Title screen and matchmaking
│   ├── TitleScreenManager.cs
│   └── NetworkRunnerHandler.cs  # Session management & callbacks
└── UI/             # UI controllers and input
    ├── UIController.cs          # Singleton, updates all game UI
    └── PlayerInputController.cs
```

### Singleton Pattern Usage

Both `GameManager` and `UIController` use the singleton pattern with `public static Instance`:

```csharp
public static GameManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this) Destroy(gameObject);
    else Instance = this;
}
```

Other scripts reference them via `GameManager.Instance` or `UIController.Instance`.

## Scene Flow

1. **TitleScene**:
   - Player enters name (stored in PlayerPrefs)
   - Clicks "Play" → NetworkRunnerHandler.StartGame()
   - Matchmaking UI shows while waiting for 2nd player
   - When 2 players join → 1 second delay → LoadScene("GameScene")

2. **GameScene**:
   - Both NetworkPlayer objects persist from TitleScene
   - GameManager spawns 2 UyopyonState objects (one per player)
   - UIController displays both players' names and stats
   - Game logic begins (turn-based system in development)

## Key Architectural Decisions

### Why Render() Instead of OnChanged?

The codebase uses Fusion's `Render()` method with manual change detection rather than `[Networked(OnChanged = ...)]` callbacks. This pattern:
- Executes on all clients every render frame
- Allows safe UI updates without replication timing issues
- Stores `_lastWeight`, `_lastEnergy` etc. to detect changes manually

Example from UyopyonState.cs:
```csharp
public override void Render()
{
    if (_lastWeight != Weight)
    {
        UIController.Instance.UpdateWeightDisplay(OwnerPlayer, Weight);
        _lastWeight = Weight;
    }
}
```

### WebGL Networking Stability

The 1-second delay before scene transition in `NetworkRunnerHandler.DelayedSceneLoad()` exists specifically for WebGL builds to ensure client connections are fully stable before loading the game scene.

### Player Identification

Players are identified via Fusion's `PlayerRef` type (not GameObject references). Dictionaries use `PlayerRef` as keys to map players to their state objects and UI elements.

## Common Development Tasks

### Adding New Networked Properties

1. Add `[Networked]` property to relevant NetworkBehaviour (e.g., UyopyonState)
2. Add corresponding `_last` field for change detection
3. Update `Render()` method to detect changes and trigger UI updates
4. Update UIController method to display the new property

### Modifying Game Logic

Game logic should be implemented in `GameManager` or `TurnProcessor` and gated with:
```csharp
if (Runner.IsServer) // or Runner.IsSharedModeMasterClient
{
    // Host-authoritative logic here
}
```

### Testing Multiplayer Locally

The `NetworkRunnerHandler.Start()` method includes editor-only auto-start code:
```csharp
if (Application.isEditor && !_started)
{
    _ = StartGame(Fusion.GameMode.Shared, "RANDOM_POOL_UYOPYON");
}
```

Build two instances or test with Unity Editor + WebGL build to simulate 2 players.
