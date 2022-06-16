#nullable enable
#r "System.Runtime"
#r "C:/Users/yuno/build/OpenNefia.NET/OpenNefia.EntryPoint/bin/Debug/net6.0/OpenNefia.Core.dll"
#r "C:/Users/yuno/build/OpenNefia.NET/OpenNefia.EntryPoint/bin/Debug/net6.0/Resources/Assemblies/OpenNefia.Content.dll"

using OpenNefia.Core.Log;
using OpenNefia.Core.Maths;
using OpenNefia.Core.IoC;
using OpenNefia.Core.Maps;
using OpenNefia.Core.Locale;
using OpenNefia.Core.Game;
using OpenNefia.Core.Utility;
using OpenNefia.Core.GameObjects;
using OpenNefia.Content.Prototypes;
using OpenNefia.Content.RandomText;
using OpenNefia.Content.Debug;
using OpenNefia.Content.Nefia;

var _maps = IoCManager.Resolve<IMapManager>();
var _script = EntitySystem.Get<ScriptTools>();

var area = _script.GetOrCreateArea("TestArea", new("Elona.NefiaDungeon"), null);
var gen = new NefiaFloorGenerator();
var mapId = new MapId(999);

var found = gen.TryToGenerate(area, mapId, 1);
var map = _maps.GetMap(mapId);
return _script.PrintMap(map);
