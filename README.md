# RecallMate 🧠

**Your personal, privacy-first activity memory for Windows.**

RecallMate quietly records the titles of the apps and windows you use throughout the day, stores everything locally, and gives you a fast, searchable timeline of your digital life. Want to know what you were working on last Tuesday at 3 PM? Just search.

Optionally connect to **Gemini**, **OpenAI**, or any **OpenAI-compatible API** (Ollama, LM Studio, etc.) to ask natural language questions about your activity history.

---

## ✨ Features

- 🔍 **Instant search** — Find any app, document, or website title from your history in milliseconds
- 📅 **Timeline view** — Grouped by day, newest first, with expandable cards
- 🔒 **100% local storage** — Everything lives in a SQLite database on your machine. Nothing leaves without your consent
- 🤖 **Optional cloud AI** — Bring your own API key for Gemini, OpenAI, or a local Ollama instance to ask questions like *"What did I work on Monday afternoon?"*
- 🪶 **Ultra lightweight** — Idles at under 100 MB RAM. No Electron, no bloat
- 🚫 **No account required** — No sign-ups, no telemetry, no tracking
- 🧹 **Smart deduplication** — Same window for 5+ minutes? Only one entry is stored, keeping your timeline clean

---

## 🖥️ Screenshots

<p align="center">
  <em>Main window with timeline, search, and AI assistant panel</em><br>
  <img src="docs/screenshot-main.png" alt="RecallMate Main Window" width="800">
</p>

<p align="center">
  <em>Settings window — connect your own AI provider</em><br>
  <img src="docs/screenshot-settings.png" alt="RecallMate Settings" width="600">
</p>

---

## 🚀 Getting Started

### Prerequisites

- Windows 10 or 11 (64-bit)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for building from source)

### Option 1: Download Pre-built Release

1. Go to the [Releases](https://github.com/yourusername/RecallMate/releases) page
2. Download `RecallMate_Setup.exe` or the portable ZIP
3. Run the app — no installation required for the portable version

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/RecallMate.git
cd RecallMate

# Build and run
dotnet run --project RecallMate
