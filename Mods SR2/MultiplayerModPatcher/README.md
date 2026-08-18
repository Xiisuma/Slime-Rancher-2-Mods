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
| A modded slime or plort cannot be described in a packet | Gives every modded identifiable type a persistence id, in reference-id order, so both machines compute the same one |
| Ranching Together built its actor table before a content mod registered | Rebuilds that table once the save is running |
| Slimes and resources hanging in mid-air once players walk away from each other | Hands an actor whose owner has gone silent to a player who can still see it |

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
