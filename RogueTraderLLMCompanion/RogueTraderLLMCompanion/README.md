# LLM Companion - Rogue Trader Combat AI

An AI companion mod for **Warhammer 40,000: Rogue Trader** that uses Large Language Models (GPT-4, Gemini, Claude) to control party members in combat with expert-level tactical decision making.

## Features

- 🤖 **AI-Controlled Combat** - Let an LLM make tactical decisions for your companions
- 🎯 **Smart Targeting** - AI analyzes threats, positions, and battlefield conditions
- ⚡ **Multiple LLM Providers** - Google Gemini, OpenAI GPT, Anthropic Claude, or local Ollama
- 🎮 **Customizable Control** - Control all companions, specific characters, or even the player
- 🔄 **Auto/Manual Modes** - Automatic execution or approve each action
- 💬 **Combat Commentary** - AI explains its tactical reasoning

## Quick Start

1. **Install Prerequisites** (see [BUILD_GUIDE.md](BUILD_GUIDE.md))
2. **Build the mod**: `dotnet build -c Release`
3. **Copy to mods folder**: Automatic on Release build
4. **Configure API key** in game (Ctrl+F10)
5. **Enter combat** and watch the AI play!

## Configuration

| Setting | Description |
|---------|-------------|
| LLM Provider | Google/OpenAI/Anthropic/Local |
| API Key | Your API key for the selected provider |
| Control Mode | All Companions / Specific / Player Too |
| Combat Style | Aggressive/Defensive/Balanced/Support |
| Execution | Auto (immediate) / Manual (approve) |

## Supported LLM Providers

| Provider | Models | Free Tier |
|----------|--------|-----------|
| Google Gemini | gemini-2.0-flash, 1.5-flash | ✅ Yes |
| OpenAI | gpt-4o-mini, gpt-4o | ❌ No |
| Anthropic | claude-3-haiku, claude-3-sonnet | ❌ No |
| Local (Ollama) | llama3.2, mistral, etc. | ✅ Self-hosted |

## Documentation

- [BUILD_GUIDE.md](BUILD_GUIDE.md) - Complete build and installation instructions
- [GAME_API_REFERENCE.md](GAME_API_REFERENCE.md) - Verified game API documentation

## Project Structure

```
RogueTraderLLMCompanion/
├── Main.cs                 # Mod entry point
├── Settings.cs             # User settings
├── Core/
│   ├── LLMService.cs       # API communication
│   └── PromptBuilder.cs    # Combat prompt generation
├── Combat/
│   ├── CombatController.cs # Main orchestration
│   ├── CombatStateExtractor.cs # Game state reading
│   ├── ActionExecutor.cs   # Action execution
│   └── TurnHooks.cs        # Harmony patches
├── Models/                 # Data structures
└── UI/                     # Settings & overlay
```

## Requirements

- Warhammer 40,000: Rogue Trader (Steam/GOG)
- .NET SDK 6.0+ with .NET Framework 4.7.2 targeting pack
- Visual Studio 2022 (recommended) or VS Code
- LLM API key (Gemini free tier works great!)

## License

MIT License - See LICENSE file

## Credits

- API discovery via [ToyBox-RogueTrader](https://github.com/xADDBx/ToyBox-RogueTrader)
- Built with [HarmonyLib](https://github.com/pardeike/Harmony)
- Uses [Unity Mod Manager](https://github.com/newman55/unity-mod-manager)
