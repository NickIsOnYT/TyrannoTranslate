# TyrannoTranslate

A desktop translator for **TyranoScript** `.ks` scenario files (similar in workflow to [Translator++](https://dreamsavior.net/translator-plusplus/) for RPG Maker).

## Features

- Spreadsheet-style grid: **Original** (left) and **Translation** (right)
- Parses Tyrano tags `[like this]` — they must stay identical in your translation
- Skips pure command lines (`[bg ...]`, `[jump ...]`, labels `*feed`, etc.)
- Includes dialogue, narration `（...）`, and character headers `#名前`
- Save writes translations back into the `.ks` file (use **Save As** to keep the original)

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)

## Run

```bash
cd TyrannoTranslate
dotnet run --project TyrannoTranslate
```

Or open `TyrannoTranslate.sln` in Visual Studio and press F5.

## Single-file build (Windows x64)

```bash
dotnet publish TyrannoTranslate/TyrannoTranslate.csproj -c Release -r win-x64
```

Output: `TyrannoTranslate/bin/Release/net8.0-windows/win-x64/publish/TyrannoTranslate.exe`

## Usage

1. **File → Open** and choose a `.ks` file (e.g. `data/scenario/event.ks`).
2. Type English in the **Translation** column. Copy `[p]`, `[lr]`, `[l]` and other tags from the original line.
3. **Edit → Copy original → translation** fills the right column from the left (handy as a starting point).
4. **File → Save** or **Save As** when bracket tags match (✓ in **St** column; `!` means a mismatch).
5. On save, the current on-disk file is copied to `filename.ks.bak` before overwriting (toggle via **Edit → Create .bak backup on save**).

## Example

| Original | Translation |
|----------|-------------|
| `絶対的なゲーム[p]` | `Absolute Gaming[p]` |

Do **not** change text inside `[brackets]` unless it is dialogue/narration outside those tags.
