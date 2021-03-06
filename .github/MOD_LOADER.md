This module was created to better understand MoonSharp (Lua) for implementing modding into a game. The project has evolved into a template to be used in other Godot C# games to add Lua modding support.

Learn Lua: https://www.lua.org/pil/contents.html  
Learn MoonSharp: https://www.moonsharp.org/getting_started.html  

#### Features
- Mods all share one Lua environment
- Mods can register callbacks without overwriting other mod callbacks
- Callbacks support params
- ModLoader can register function hooks anywhere
- Mods can see objects like Player and do stuff like Player:setHealth(x)
- Lua Debugger support
- Safety null checks to see if all properties were filled out in mod info.json
- Individual mods can be enabled or disabled
- Mod name, description, author, version, game versions, dependencies displayed in UI

#### Hooks
```cs
public override void _Ready()
{
    // if we had a Player class defined with SetHealth function, this is how you would link that function with Lua
    ModLoader.Script.Globals["Player", "setHealth"] = (Action<int>)D_Master.Player.SetHealth;

    ModLoader.Call("OnGameInit");
}

public override void _Process(float delta)
{
    ModLoader.Call("OnGameUpdate", delta);
}
```

#### Mod Structure
info.json
```json
{
    "name": "ModTest",
    "version": "0.0.1",
    "author": "Foo",
    "dependencies": ["Bar"],
    "description": "Example mod",
    "gameVersions": []
}
```

script.lua
```lua
-- example script of what you can do

RegisterCallback('OnGameInit', nil, function()
    print('Game start')
end)

local x = 0

RegisterCallback('OnGameUpdate', nil, function(delta)
    print('Delta', delta)
    Player:setHealth(x)
    x = x + 1
end)
```

Mod file structure
```
|-Mods
|--Foo
|---info.json
|---script.lua
|--Bar
|---info.json
|---script.lua
```

Mods Directory Location
- Exported Releases: `${GameExecutable}/Mods/...`
- Non-Exported Releases: `res://Mods/...`

Lua Scripts Location
- `res://Scripts/Lua/...`

#### Note
Just found out that this is a thing https://docs.godotengine.org/en/3.4/tutorials/export/exporting_pcks.html although as of writing this it will not work with C# projects because of this issue https://github.com/godotengine/godot/issues/36828. If your project is 100% GDScript then by all means use PCK files instead of this.
