# LLM Companion Mod - Build & Installation Guide

This guide will walk you through compiling and installing the LLM Companion mod for Warhammer 40,000: Rogue Trader.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Game Installation](#game-installation)
3. [Development Environment Setup](#development-environment-setup)
4. [Project Setup](#project-setup)
5. [Building the Mod](#building-the-mod)
6. [Installing the Mod](#installing-the-mod)
7. [Configuration](#configuration)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| .NET SDK | 6.0+ (with 4.7.2 targeting pack) | [Download](https://dotnet.microsoft.com/download) |
| Visual Studio 2022 | Community or higher | [Download](https://visualstudio.microsoft.com/) |
| Warhammer 40K: Rogue Trader | Latest version | Steam/GOG |

### Required Visual Studio Workloads

When installing Visual Studio, ensure you select:
- **.NET desktop development**
- **.NET Framework 4.7.2 targeting pack** (in Individual Components)

### One-Line Install (Windows Command Prompt)

```batch
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.VisualStudio.2022.Community --override "--add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.Net.Component.4.7.2.SDK --add Microsoft.Net.Component.4.7.2.TargetingPack"
```

---

## Game Installation

### Step 1: Install Rogue Trader

Install the game through Steam or GOG. Default paths:

- **Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Warhammer 40000 Rogue Trader`
- **GOG**: `C:\GOG Games\Warhammer 40000 Rogue Trader`

### Step 2: Run the Game Once

Launch the game at least once to:
- Generate necessary config files
- Create the `Player.log` file (required for mod templates)
- Initialize Unity Mod Manager (built into the game)

### Step 3: Locate Game Assemblies

The DLLs you need are in:
```
<GamePath>/WH40KRT_Data/Managed/
```

Key assemblies:
- `Assembly-CSharp.dll` - Main game code
- `UnityEngine.dll` - Unity engine
- `UnityEngine.CoreModule.dll` - Unity core
- `0Harmony.dll` - Harmony library (pre-included)
- `Newtonsoft.Json.dll` - JSON library (pre-included)

---

## Development Environment Setup

### Option A: Using Owlcat NuGet Templates (Recommended)

This is the easiest method - it auto-configures everything.

```batch
:: Open Command Prompt as Administrator

:: 1. Add NuGet source (if not already added)
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

:: 2. Install Owlcat templates
dotnet new install Owlcat.Templates

:: 3. Create new mod project (run in your workspace folder)
cd C:\Users\YourName\Workspace
dotnet new rtmod -n RogueTraderLLMCompanion -D "LLM Companion"
```

Then copy the source files from this project into the generated project.

### Option B: Manual Setup (Current Project)

#### Step 1: Update Project File

Edit `RogueTraderLLMCompanion.csproj` to point to your game installation:

```xml
<PropertyGroup>
    <!-- UPDATE THIS PATH to your game installation -->
    <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Warhammer 40000 Rogue Trader</GamePath>
    <ManagedPath>$(GamePath)\WH40KRT_Data\Managed</ManagedPath>
</PropertyGroup>
```

#### Step 2: Verify Assembly References

The project file should reference these DLLs from the game:

```xml
<ItemGroup>
    <Reference Include="Assembly-CSharp">
        <HintPath>$(ManagedPath)\Assembly-CSharp.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
        <HintPath>$(ManagedPath)\UnityEngine.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
        <HintPath>$(ManagedPath)\UnityEngine.CoreModule.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
        <HintPath>$(ManagedPath)\0Harmony.dll</HintPath>
        <Private>false</Private>
    </Reference>
    <Reference Include="Newtonsoft.Json">
        <HintPath>$(ManagedPath)\Newtonsoft.Json.dll</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

---

## Project Setup

### Step 1: Clone or Download the Project

```batch
cd C:\Users\YourName\Workspace
git clone <repository-url> RogueTraderLLMCompanion
```

Or download and extract the ZIP file.

### Step 2: Open in Visual Studio

1. Open `RogueTraderLLMCompanion.sln` in Visual Studio 2022
2. Wait for NuGet package restore to complete
3. If there are errors about missing assemblies, verify your `<GamePath>` in the .csproj file

### Step 3: Remove Debug Flag (For Game Build)

The code uses conditional compilation. For building against the actual game:

1. Open project properties (right-click project → Properties)
2. Go to **Build** tab
3. Remove `DEBUG_WITHOUT_GAME` from **Conditional compilation symbols**

Or edit the .csproj:
```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <DefineConstants></DefineConstants>  <!-- Remove DEBUG_WITHOUT_GAME -->
</PropertyGroup>
```

---

## Building the Mod

### Method 1: Visual Studio

1. Set configuration to **Release**
2. Build → Build Solution (Ctrl+Shift+B)
3. Output will be in `bin/Release/net472/`

### Method 2: Command Line

```batch
cd C:\Users\YourName\Workspace\RogueTraderLLMCompanion
dotnet build -c Release
```

### Method 3: With Auto-Install

Add this to your .csproj to auto-copy the mod after building:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent" Condition="'$(Configuration)'=='Release'">
    <PropertyGroup>
        <ModFolder>$(GamePath)\Mods\RogueTraderLLMCompanion</ModFolder>
    </PropertyGroup>
    <MakeDir Directories="$(ModFolder)" />
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(ModFolder)" />
    <Copy SourceFiles="$(TargetDir)Info.json" DestinationFolder="$(ModFolder)" />
</Target>
```

### Build Output

After successful build, you should have:
```
bin/Release/net472/
├── RogueTraderLLMCompanion.dll     (main mod assembly)
├── Info.json                        (mod metadata)
└── (other dependencies)
```

---

## Installing the Mod

### Step 1: Create Mod Folder

Create a folder for your mod in the game's Mods directory:

```batch
mkdir "<GamePath>\Mods\RogueTraderLLMCompanion"
```

Example:
```batch
mkdir "C:\Program Files (x86)\Steam\steamapps\common\Warhammer 40000 Rogue Trader\Mods\RogueTraderLLMCompanion"
```

### Step 2: Copy Files

Copy these files from `bin/Release/net472/` to the mod folder:

```
RogueTraderLLMCompanion/
├── RogueTraderLLMCompanion.dll     (required)
├── Info.json                        (required)
└── settings.json                    (optional, created on first run)
```

### Step 3: Enable the Mod

1. Launch Rogue Trader
2. Press **Ctrl+F10** to open Unity Mod Manager
3. Find "LLM Companion" in the mod list
4. Check the box to enable it
5. Click "Apply"

---

## Configuration

### First-Time Setup

1. In-game, press **Ctrl+F10** to open Unity Mod Manager
2. Click on "LLM Companion" to expand settings
3. Configure your LLM provider:

### LLM Provider Configuration

#### Google Gemini (Recommended)
```
Provider: Google
Model: gemini-2.0-flash
API Key: <your-api-key>
```

Get API key: https://aistudio.google.com/apikey

#### OpenAI
```
Provider: OpenAI
Model: gpt-4o-mini
API Key: <your-api-key>
```

Get API key: https://platform.openai.com/api-keys

#### Anthropic Claude
```
Provider: Anthropic
Model: claude-3-haiku-20240307
API Key: <your-api-key>
```

Get API key: https://console.anthropic.com/settings/keys

#### Local LLM (Ollama)
```
Provider: Local
Model: llama3.2
Custom Endpoint: http://localhost:11434/api/generate
```

### Combat Settings

| Setting | Description |
|---------|-------------|
| Control All Companions | AI controls all party members |
| Control Player Character | AI also controls main character |
| Combat Style | Aggressive/Defensive/Balanced/Support (hint for AI) |
| Execution Mode | Auto (immediate) or Manual (approval required) |
| Use Heroic Acts | Allow AI to use momentum abilities |

---

## Troubleshooting

### Build Errors

#### "Could not find reference assembly"
- Verify `<GamePath>` in .csproj points to your game installation
- Ensure the game is installed and has been run at least once
- Check that DLLs exist in `<GamePath>\WH40KRT_Data\Managed\`

#### "Target framework not installed"
- Install .NET Framework 4.7.2 Developer Pack
- In Visual Studio Installer, add ".NET Framework 4.7.2 targeting pack"

#### "HarmonyLib not found"
- The game includes Harmony. Reference it from:
  `<GamePath>\WH40KRT_Data\Managed\0Harmony.dll`

### Runtime Errors

#### Mod doesn't appear in UMM
- Verify `Info.json` is in the mod folder
- Check the `Info.json` has correct format and valid JSON

#### "LLM request failed"
- Check your API key is correct
- Verify internet connection
- Check the game's Player.log for detailed errors:
  `%APPDATA%\..\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Player.log`

#### AI doesn't take turns
- Enable the mod in UMM (Ctrl+F10)
- Check "Control All Companions" or add specific companions
- Verify you're in turn-based combat

### Logs Location

Game logs (including mod errors):
```
%LOCALAPPDATA%Low\Owlcat Games\Warhammer 40000 Rogue Trader\Player.log
```

Or:
```
C:\Users\<YourName>\AppData\LocalLow\Owlcat Games\Warhammer 40000 Rogue Trader\Player.log
```

---

## Quick Reference

### File Locations

| What | Path |
|------|------|
| Game | `C:\Program Files (x86)\Steam\steamapps\common\Warhammer 40000 Rogue Trader` |
| Game DLLs | `<GamePath>\WH40KRT_Data\Managed\` |
| Mods Folder | `<GamePath>\Mods\` |
| Settings | `<GamePath>\Mods\RogueTraderLLMCompanion\settings.json` |
| Game Log | `%LOCALAPPDATA%Low\Owlcat Games\Warhammer 40000 Rogue Trader\Player.log` |

### Commands

```batch
# Build Release
dotnet build -c Release

# Clean and rebuild
dotnet clean && dotnet build -c Release

# Restore packages
dotnet restore
```

### Keyboard Shortcuts (In-Game)

| Key | Action |
|-----|--------|
| Ctrl+F10 | Open Unity Mod Manager |
| Ctrl+M | Open Mod GUI (if available) |

---

## Next Steps

After installation:
1. Enter combat to test the mod
2. Watch the console (Ctrl+F10) for AI decisions
3. Adjust settings based on AI performance
4. Report issues with Player.log attached

For updates and support, check the mod's repository.
