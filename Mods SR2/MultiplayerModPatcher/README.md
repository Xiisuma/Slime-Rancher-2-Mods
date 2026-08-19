<h1 align="center">Multiplayer Mod Patcher</h1>

<p align="center">
  <b>Repairs what Ranching Together cannot synchronise on its own.</b><br>
  Modded content over the network, and actors nobody is simulating any more.
</p>

---

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) 0.7.x for Slime Rancher 2 and run the game once.
2. Drop `MultiplayerModPatcher.dll` into the game's `Mods` folder.

**Every player in the session installs it**, along with the same set of content mods. It reads
[Ranching Together](https://www.nexusmods.com/slimerancher2/mods/118) by reflection and never
references it: without the multiplayer mod, the patcher says so in the log and stays idle.

## What it fixes

| Problem | What the patcher does |
|---|---|
| A crop planted by the other player is remembered as the wrong patch, or not planted at all | Reads the grower definition off the patch that was actually planted instead of guessing it from a hash table |
| A zone's forecast is built from another zone's weather | Resolves the pattern from the zone's own configuration, and drops what still belongs elsewhere |
| What a client sends is numbered by its own table, not the host's | Translates outgoing type ids through the table the host handed over on connection |
| Market prices land on the wrong plorts | Packs and unpacks the price update in reference-id order instead of by hash-table position |
| A modded slime or plort cannot be described in a packet | Gives every modded identifiable type a persistence id, in reference-id order, so both machines compute the same one |
| Ranching Together built its actor table before a content mod registered | Rebuilds that table once the save is running |
| A weather update that fails to apply says nothing | Writes down what the packet carried, so the entry that breaks it can be named |
| Slimes and resources hanging in mid-air once players walk away from each other | Hands an actor whose owner has gone silent to a player who can still see it |

## The market board

Ranching Together sends the board as a bare array of `(current, previous)` pairs and applies it by
position, walking the host's `PlortEconomyDirector._currValueMap` and the client's in parallel:

```csharp
foreach (var item in plortEconomyDirector._currValueMap._entries)   // SR2MP 0.3.8, MarketPriceHandler
{
    if (item.value != null)
        (item.value.CurrValue, item.value.PrevValue) = packet.Prices[num];
    num++;
}
```

Nothing in the packet says which plort a price is for. That map is a hash table, and a modded plort
is inserted into it at load time, so the two machines lay their tables out differently — both boards
then show the same numbers on different plorts.

The patcher keeps the wire format and changes the order both sides read it in: sorted by reference
id, which is the same sequence on every machine running the same mods. A length mismatch means the
other side is not running the same set, and the update is dropped with a line in the log rather than
applied to the wrong plorts.

## The gardens

Ranching Together works out what a plot is growing by scanning the raw storage behind the game's
grower translation and taking the first entry whose primary resource is the crop:

```csharp
landPlotModel.resourceGrowerDefinition = translation._resourceGrowerTranslation
    .RawLookupDictionary._entries
    .FirstOrDefault(x => x.value._primaryResourceType == actor).value;   // SR2MP 0.3.8
```

Every crop has two definitions — the normal patch and the deluxe one — and which comes first is
whatever order the hash table stores them in, so a plot can end up remembering the wrong one. And
`_entries` is the raw array behind the dictionary: the slots past the last insertion hold no value,
so a crop with no matching definition — a modded one the other player has and this game does not —
walks into them and throws, which costs the whole update rather than the grower alone.

The patcher plants the crop first and reads the definition back off the patch that was planted,
which is the game's own answer and needs no guess about deluxe. A plot too far away to be loaded
falls back to a lookup that picks by patch prefab rather than by hash order.

## The weather

Ranching Together maps a weather state to the pattern that plays it through a lookup built once,
from whatever the zone configurations held at that moment. The state lists those patterns play are
filled as zones come to life, so most of them are still empty when the snapshot is taken — and
rather than say a state is unknown, the lookup falls back to any pattern with the same state name,
which is always the one loaded first:

```
Using fallback pattern for Luminous Strand / Rain Light State: Rain Pattern Fields   # SR2MP 0.3.8
```

One evening of two-player ranching produced 2 177 of those lines. The forecasts they build name a
pattern belonging to another zone, and the game throws when it reads them to work out what to draw
on the map — 96 times in the same session, from `WeatherRegistry.CalculateZoneMapData`, besides the
three updates lost outright inside `NetworkWeatherManager.Apply`.

The patcher answers the same question from the configurations loaded right now: the zone's own
pattern for that state, or failing that its pattern for the same weather, matched on the metadata the
game uses to describe one. Whatever still ends up in a forecast is checked once the update has been
applied, and an entry naming another zone's pattern is dropped rather than left for the map to read.

## The ids a client sends back

Ranching Together names an actor's kind by the index the save system gives its identifiable type, and
those indexes are per-machine: one extra content mod renumbers everything after it. The mod handles
one direction — on connection the host sends its whole table as reference ids and the client rewrites
its lookup to match — but what the client sends back is numbered by its own table:

```csharp
public static int GetPersistentID(IdentifiableType type) =>          // SR2MP 0.3.8
    SRSingleton<GameContext>.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);
```

While both players run exactly the same mods the two numberings coincide and nothing shows. As soon
as they differ, everything the client sends arrives on the host as the wrong kind of thing.

The patcher keeps the table the host sent and puts a client's outgoing ids through it. A type the
host has no id for is named once in the log rather than sent as something else, and the actor table
is left alone while connected, since it holds the host's numbering rather than this game's.

## The floating actors

Ranching Together freezes the rigidbody of every actor it does not own locally
(`RigidbodyConstraints.FreezeAll`) and moves it from the packets its owner sends. When the owner
walks far enough away for its region to hibernate, it stops sending and hands the actor back with an
unload packet — but that hand-back is dropped whenever the actor has no `RegionMember`:

```csharp
if (!component.RegionMember) return false;   // SR2MP 0.3.8, ActorUnloadHandler
```

and its periodic sweep only reclaims actors that are unowned, awake, and within 600 units. An actor
outside all of that keeps naming an owner who no longer simulates it — frozen, silent, hanging where
it was.

The patcher applies the rule Ranching Together already believes in — an owner nobody has heard from
has lost the actor — to every remote actor the local player is close enough to run, and claims it.
Ranching Together then unfreezes the rigidbody itself, on the ownership change it was waiting for.

## Settings

```ini
[MultiplayerModPatcher]
claimAbandonedActors = true
snapshotHotkey = "F10"
```

`claimAbandonedActors = false` keeps the reporting and touches nothing, which is how to tell a fix
from a coincidence: the log still counts what it would have claimed.

The snapshot key writes the network state of the actors around you to the log. Press it while looking
at something stuck in mid-air and the line that describes it says which part is stuck:

```
[Multiplayer_Mod_Patcher] Describing up to 25 networked actors:
  owner=PLAYER_A670CE1E3 mine=False heard=False hibernating=n/a frozen=FreezeAll at=(461.0, 14.0, 341.2)
```

`mine=False heard=False frozen=FreezeAll` is the bug: an actor this game draws, frozen, whose owner
has not been heard from. `hibernating=n/a` means the actor has no `RegionMember` — the case Ranching
Together's hand-back drops.

```
[Multiplayer_Mod_Patcher] Ranching Together found (v0.3.8.0).
[Multiplayer_Mod_Patcher] Watchdog installed on Ranching Together's actors.
[Multiplayer_Mod_Patcher] Gardens keep the grower definition the crop actually grows from.
[Multiplayer_Mod_Patcher] Weather forecasts are kept to each zone's own patterns.
[Multiplayer_Mod_Patcher] Actor types are sent to the host under the host's own ids.
[Multiplayer_Mod_Patcher] Ranching Together actor table refreshed: 0 types added, 770 known.
[Multiplayer_Mod_Patcher] Watchdog: 3 actors left without a live owner, 3 claimed back.
```

## Notes

- Ranching Together names an actor's type by the index the save system gives it, and translates the
  host's indexes to the client's on connection — but sends its own local index back the other way.
  Two players therefore need the same indexes, which is why the modded ones are handed out in
  reference-id order rather than in whatever order MelonLoader happened to load the mods in.
- Nothing is referenced at compile time. Every member of the multiplayer mod is looked up by name;
  one missing member disables the affected half with a line in the log rather than throwing mid
  session.

## Build

Source lives in [`Source/MultiplayerModPatcher`](../../Source/MultiplayerModPatcher). Point the build at a Slime Rancher 2 install
with MelonLoader on it — `SR2_PATH`, or `-p:GamePath="…\Slime Rancher 2"` — then:

```bash
dotnet build "Source/MultiplayerModPatcher/MultiplayerModPatcher.csproj" -c Release
```

## Credits

Not a port: this mod was written by **Xiu_ma**, **PikaCat** and **Claude** (Anthropic) to keep the
adapted Slime Rancher 1 mods working in multiplayer.

It patches **[Ranching Together](https://www.nexusmods.com/slimerancher2/mods/118)** by **py8** from
the outside and contains none of its code; credit for the multiplayer mod stays with its author.
