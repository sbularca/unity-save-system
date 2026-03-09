# Jovian Save System

A generic, game-agnostic save system for Unity supporting multiple save slots, sessions, auto-saves, and dual JSON/Binary serialization.

## Requirements

- Unity 2022.3+
- Newtonsoft Json (`com.unity.nuget.newtonsoft-json` 3.2.1)

## Installation

Add as a git submodule or reference via Unity Package Manager:

```
https://github.com/sbularca/unity-save-system.git
```

## Architecture

```
ISaveSystem (facade)
├── ISaveSlotManager  — session and slot allocation
├── ISaveSerializer   — data encoding (JSON or Binary)
└── ISaveStorage      — byte-level file I/O
```

All components are interface-driven and injected via constructor, making them easy to swap or mock for testing.

## Configuration

Settings are accessible in the Unity Editor under **Project Settings > Jovian > Save System**.

| Setting | Default | Description |
|---------|---------|-------------|
| Save Format | Json | Serialization format (Json or Binary) |
| Max Auto Saves | 3 | Auto-save slots before rotation begins |
| Save Version | 1 | Version number embedded in each save for migration |
| Save Directory | "saves" | Subfolder under `Application.persistentDataPath` |
| Obfuscation Key | "default-key" | XOR key used by the Binary serializer |

## Save Slot Types

| Type | Behavior |
|------|----------|
| **Manual** | Player-initiated. Each save gets a new slot (manual_001, manual_002, ...) |
| **Auto** | System-initiated. Creates slots up to the configured max, then rotates the oldest |
| **Quick** | Single slot per session. Always overwrites the previous quick save |

## Quick Start

### 1. Define your save data

```csharp
public class GameState {
    public string playerName;
    public int level;
    public float health;
}
```

### 2. Wire up the save system

```csharp
SaveSystemSettings settings = SaveSystemSettings.Load();

ISaveStorage storage = new FileSystemSaveStorage(
    Application.persistentDataPath, settings.saveDirectoryName);

ISaveSerializer serializer = settings.saveFormat == SaveFormat.Binary
    ? new BinarySaveSerializer(settings.obfuscationKey)
    : new JsonSaveSerializer();

ISaveSlotManager slotManager = new SaveSlotManager(storage, settings);

ISaveSystem saveSystem = new SaveSystem(serializer, storage, slotManager, settings);
```

### 3. Create a session and save

```csharp
string sessionId = saveSystem.CreateSession();

GameState state = new GameState {
    playerName = "Hero",
    level = 10,
    health = 95.5f
};

// Manual save
saveSystem.Save(sessionId, state, SaveSlotType.Manual);

// Auto save
saveSystem.Save(sessionId, state, SaveSlotType.Auto);

// Quick save
saveSystem.Save(sessionId, state, SaveSlotType.Quick);
```

### 4. Load a save

```csharp
IReadOnlyList<SaveSlotInfo> slots = saveSystem.GetSlots(sessionId);

foreach(SaveSlotInfo slot in slots) {
    Debug.Log($"{slot.DisplayLabel} — {slot.TimestampDateTime}");
}

GameState loaded = saveSystem.Load<GameState>(slots[0]);
```

### 5. Async save/load

```csharp
await saveSystem.SaveAsync(sessionId, state, SaveSlotType.Auto);

GameState loaded = await saveSystem.LoadAsync<GameState>(slots[0]);
```

## Session Management

Sessions group related saves together (e.g. one playthrough).

```csharp
// List all sessions
IReadOnlyList<SaveSessionInfo> sessions = saveSystem.GetAllSessions();

foreach(SaveSessionInfo session in sessions) {
    Debug.Log($"Session {session.sessionId} — Last save: {session.LastSaveDateTime}");
}

// Check if any saves exist
bool hasSaves = saveSystem.HasAnySaves();

// Delete a single slot
saveSystem.DeleteSlot(slots[0]);

// Delete an entire session and all its saves
saveSystem.DeleteSession(sessionId);
```

## Serialization Formats

### JSON (`JsonSaveSerializer`)

Human-readable, indented JSON. Ideal for development and debugging. Save files are plain text and easy to inspect.

### Binary (`BinarySaveSerializer`)

Compact binary format that applies XOR obfuscation with a configurable key followed by Deflate compression. Recommended for release builds — smaller file size and not trivially editable.

## Testing

The interfaces make it easy to test with an in-memory storage implementation:

```csharp
public class InMemorySaveStorage : ISaveStorage {
    private readonly Dictionary<string, byte[]> files
        = new Dictionary<string, byte[]>();

    public void Write(string path, byte[] data) { files[path] = data; }
    public byte[] Read(string path) { return files[path]; }
    public bool Exists(string path) { return files.ContainsKey(path); }
    public void Delete(string path) { files.Remove(path); }
    public string[] List(string dir) {
        return files.Keys.Where(k => k.StartsWith(dir)).ToArray();
    }
    public void CreateDirectory(string path) { }

    // Async versions delegate to sync
    public Task WriteAsync(string path, byte[] data) { Write(path, data); return Task.CompletedTask; }
    public Task<byte[]> ReadAsync(string path) => Task.FromResult(Read(path));
    public Task<bool> ExistsAsync(string path) => Task.FromResult(Exists(path));
    public Task DeleteAsync(string path) { Delete(path); return Task.CompletedTask; }
    public Task<string[]> ListAsync(string dir) => Task.FromResult(List(dir));
}
```

Then wire it up in your test:

```csharp
InMemorySaveStorage storage = new InMemorySaveStorage();
SaveSystemSettings settings = new SaveSystemSettings();
JsonSaveSerializer serializer = new JsonSaveSerializer();
SaveSlotManager slotManager = new SaveSlotManager(storage, settings);
ISaveSystem saveSystem = new SaveSystem(serializer, storage, slotManager, settings);

string sessionId = saveSystem.CreateSession();
saveSystem.Save(sessionId, myData, SaveSlotType.Manual);

IReadOnlyList<SaveSlotInfo> slots = saveSystem.GetSlots(sessionId);
MyData loaded = saveSystem.Load<MyData>(slots[0]);
Assert.AreEqual(myData.playerName, loaded.playerName);
```

## License

Internal package — Jovian Industries.
