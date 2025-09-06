# PlusStudioConverterTool

## What is this?

This is a simple tool made for converting files used in the [Plus Level Studio mod](https://gamebanana.com/mods/617567).  
It works with both modern and old file formats, and can also pull out embedded assets when needed.

## Features

- **Modern formats:**
  - `.ebpl` -> editable project file.
  - `.pbpl` -> compiled file for testing in the editor.
  - `.bpl` -> compiled file for loading in-game as `LevelAsset`.
  - `.rbpl` -> room-compiled file for in-game `RoomAsset` use.

- **Legacy formats:**
  - `.bld` -> old editor files.
  - `.cbld` -> old compiled levels.

- **Asset exporting:** Extracts assets from `.ebpl` and `.pbpl`.
- **JSON Filter:** Filter out any asset inside the levels to fix invalid names or to simply remove them (really useful for modders!).

## How do I use it?

You’ve got two ways:

1. **Drag & Drop** -> Drop a file or folder onto the executable. It'll store them until your next input.
2. **CLI mode** -> Open the exe directly and use the simple menu to pick conversions or actions.

## When would I use this?

- **Bringing back old projects** -> Convert `.bld` and `.cbld` to the newer formats.
- **Recovering lost data** -> Turn `.pbpl` back into `.ebpl` if you don’t have the source anymore, for example.

## Getting it

### Users
Grab the latest compiled version from [Releases](https://github.com/PixelGuy123/PlusStudioConverterTool/releases).

### Developers
1. Clone this repo.  
2. Open in your IDE (Visual Studio works fine).  
3. Update the `.csproj` DLL references for your setup.  
   - Target framework: **.NET 9.0**

## Dependencies

- **NuGet:** SixLabors.ImageSharp  
- **Direct DLL references:**  
  - Old + new editor DLLs  
  - Unity core DLLs  
  - BepInEx DLLs  
  - Possibly others depending on your setup  

## Notes

**Some browsers or antiviruses might complain and flag it as unsafe.** This is a false-positive and only happens often because the tool is capable of deleting entire directories when setting an extract folder (be very careful though, it _can_ do that!).
