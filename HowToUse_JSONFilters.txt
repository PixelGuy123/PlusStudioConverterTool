This quiick guide explains how to create your own JSON filter files. These filters are used by the converter tool to automatically rename or exclude certain assets (like objects, items, doors, etc.) when converting older level formats (BLD, CBLD) or compiled formats (RBPL, PBPL, BPL) to the editor-readable EBPL format.

This is useful if asset names have changed between game (or mod) versions or if you want to remove obsolete assets during the conversion process.

A JSON filter file has two main jobs:
1.  Replacements: It tells the tool "when you see an asset with this old name, change it to this new name".
2.  Exclusions: It tells the tool "if you see an asset with this name, ignore it completely and do not include it in the new level file".

Each filter file targets a specific category of assets, like "Door" or "Object".

--- CREATING YOUR FILTER FILE ---

1.  Create a new text file and save it with a .json extension, for example, 'my_object_filter.json'.

2.  The file must contain three main parts: 'AreaType', 'replacements', and 'exclusions'.

Here is the structure based on the 'FilterDoorSample.json' example:
{
  "AreaType": "Door",
  "replacements": {
    "swing": "swinging",
    "swingsilent": "swinging_silent"
  },
  "exclusions": [
    "old_buggy_door"
  ]
}

Let's break down each part:

- AreaType:
This is the most important field. It tells the converter which type of asset this filter applies to. You must use one of the exact names from the list below.

Valid AreaType values are:
Structure
Object
Item
RoomTexture
Activity
Poster
Light
Door
Window
Exit
NPC

- replacements:
This is a list of "old_name": "new_name" pairs. During conversion, if the tool finds an asset of the specified 'AreaType' with a name matching an "old_name", it will change it to the "new_name".

In the example above, any door with the type "swing" will be converted into a "swinging" door.

- exclusions:
This is a simple list of asset names that should be completely ignored and skipped during conversion. If the tool finds an asset of the specified 'AreaType' with a name in this list, it will not be added to the new EBPL file.

In the example, any door with the type "old_buggy_door" will be discarded.

--- A MORE COMPLETE EXAMPLE ---

Let's say you want to create a filter for Objects. You want to rename "desk" to "teacher_desk", "table" to "student_desk", and completely ignore any objects named "obsolete_computer".

Your 'my_object_filter.json' file would look like this:

{
  "AreaType": "Object",
  "replacements": {
    "desk": "teacher_desk",
    "table": "student_desk"
  },
  "exclusions": [
    "obsolete_computer"
  ]
}

Just note there's no such thing as a "obsolete_computer" or "teacher_desk". This would likely break your EBPL, so be careful with these renamings!

--- HOW TO USE YOUR NEW FILTER ---

Just open up the tool, it'll have an option for changing JSON settings. You can, from there, add your own JSON file by inserting the path. No big deal!

--- IMPORTANT RULES ---

- An asset name cannot be in both 'replacements' and 'exclusions' in the same 'AreaType'.
- You can have multiple filter files for the same 'AreaType'. The tool will merge them. However, if there are conflicting rules for the same old asset name, the behavior might be unpredictable. It is best to keep all rules for one 'AreaType' organized.
- Do not create replacement chains across files. For example, do not have one file that changes "A" to "B" and another file that changes "B" to "C". The tool is designed to prevent this.