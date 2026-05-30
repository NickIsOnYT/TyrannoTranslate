# TyrannoTranslate

A desktop manual translator tool for **TyranoScript** `.ks` scenario files. (Made with Cursor Vibecoding)

## Features

- Spreadsheet-style grid: **Original** (left) and **Translation** (right)
- Parses Tyrano tags `[like this]` — they must stay identical in your translation.
- Skips pure command lines. (`[bg ...]`, `[jump ...]`, labels `*feed`, etc.)
- Includes dialogue, narration `（...）`, and character headers `#名前`.
- Save writes translations back into the `.ks` file.
- Backup system for the original script file and your current progress.
- An auto-population system so you don't need to copy-paste the same thing 100 times

## Developement

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)

### Run

```bash
dotnet run --project TyrannoTranslate
```

### Build

```bash
dotnet publish TyrannoTranslate/TyrannoTranslate.csproj -c Release -r win-x64
```

Output: `TyrannoTranslate/bin/Release/net8.0-windows/win-x64/publish/TyrannoTranslate.exe`

## Usage

1. **File → Open** and choose a `.ks` file (e.g. `data/scenario/event.ks`).
2. Type English in the **Translation** column. Copy `[p]`, `[lr]`, `[l]` and other tags from the original line.
3. **Edit → Copy original → translation** fills the right column from the left (handy as a starting point).
4. **File → Save** or **Save As** when bracket tags match (`✓` in **St** column; `!` means a mismatch).
5. **Backups** (each toggled under the **Edit** menu):
   - `filename.ks.bak` — copy of the on-disk file **before your first save only** (never overwritten on later saves).
   - `filename.ks.baktl` — snapshot of your current in-memory translations; **updates on every save** while enabled.
   - (If you want to restore a backup, please override the main file with one of the backup files.)
6. **Edit → Auto-populate** lets you fill any repeating dialogues/names with only a few clicks.

## Example

| # | Ln | St | Original | Translation |
|-|-|-|-|-|
| 1 | 12 | ✓ | `絶対的なゲーム[p]` | `Absolute Gaming[p]` |

Do **not** change text inside the brackets (`[p]`) unless it is dialogue/narration outside those tags.
