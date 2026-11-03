# ItemRoulette

**ItemRoulette** transforms item acquisition in Risk of Rain 2. Instead of scouring every corner of the map for scattered chests, you acquire items on the move via a dynamic on-screen HUD reel. Fight, dash, defeat bosses, and build your survivor without ever interrupting combat.

Beyond the core HUD reel, the mod introduces a European casino roulette wheel with gold and item wagers, a high-stakes "Life Bet" mechanic, and unpredictable Chaos Events.

---

## 🎮 Core Mechanics

### 🔮 Artifact of the Roulette (Artifact Edition)
ItemRoulette mechanics are integrated cleanly into the game's artifact system:
- **Run Rulebook Choice**: Select the **Artifact of the Roulette** in the character selection lobby or run rules to activate ItemRoulette's drop suppression, HUD reel, and casino features.
- **Vanilla Compatibility**: Playing a run without the artifact active operates 100% as standard vanilla Risk of Rain 2 with normal chests, printers, and boss item drops.
- **Configurable Mode**: Prefer having the roulette active in every run without selecting the artifact? Set `RequireArtifact = false` in the configuration (`00a. Client - General & UI`).

### 🎯 On-Screen Item Reel (HUD Reel)
The primary method for acquiring loot during your run:
- **Instant Spin `[B]`**: An animated item ribbon integrated directly into the upper HUD above your inventory bar. Spin on the fly—while sprinting, jumping, and shooting—without opening pause menus or stopping gameplay.
- **Dynamic Cost Scaling**: Base spin costs scale with run difficulty and increase with each paid roll on the current stage, resetting completely upon advancing to the next environment.
- **Free Spins**:
  - Fully charging the Teleporter awards a free spin (with additional bonus spins for each activated Mountain Shrine).
  - The Sale Star item grants bonus free spins at the start of each stage.
  - *Roll Stacking*: Unspent free spins carry over across stages rather than expiring.
- **Auto-Spin `[Hold V]`**: A seamless background mode. If you run out of gold, the reel pauses and automatically resumes spinning as soon as you earn enough currency.
- **Item Synergies**: Native support for Chance Doll—every rolled item has a chance to instantly upgrade its tier directly on the ribbon.

---

### 🔮 Void Items: Corruption, Reroll & Cleansing
Rolling a Void item is a pivotal choice. When the reel lands on a newly discovered corrupted item, it pauses and presents three tactical choices:
- **`[B]` Take Item**: Embrace the Void and corrupt corresponding standard items in your inventory.
- **`[R]` Reroll Item**: Test your luck again using Lunar Coins (cost and maximum attempts per item are configurable).
- **`[C]` Decline Corruption**:
  - *Via Lunar Coins*: Safely cleanse the item with no negative side effects.
  - *Via Maximum Health*: Cleanse at the cost of a temporary max HP penalty until the end of the stage (stacks cumulatively with each decline, displays native curse bar striping, and respects a configurable safety cap).

---

### 🎰 European Roulette & "Life Bet"
For survivors willing to risk their hard-earned gold and equipment:
- **Side Panel `[K]`**: A sleek European roulette interface (37 pockets: `0` to `36`) positioned cleanly so it doesn't obstruct combat visibility or crosshairs.
- **Gold Bets**: Red/Black, Even/Odd, Low (1–18)/High (19–36), Dozens, Columns, and Straight-Up numbers with authentic casino payout ratios.
- **Item Bet on Zero (0)**: Stake any item from your inventory on Zero. If the ball lands on `0`, your item is **duplicated**; otherwise, the item is destroyed.
- **☠ "Life Bet" Mode**: The ultimate gamble. Wager your survivor's very life. The payout value is dynamically calculated based on your level, stage, and the combined net worth of your inventory. Victory grants an enormous windfall of gold; defeat results in instant permadeath. Includes a two-step confirmation dialog with a safety countdown to prevent accidental clicks.

---

### 🌀 Chaos Events
Mystery question mark sectors occasionally appear on the reel, triggering dynamic game-altering events:
- **13 Boon Events (Green)**: Instant payout doubling, temporary arsenal duplication, support drone reinforcements, rarity blessings, and cash windfalls.
- **15 Hazard Events (Red)**: Price inflation, gold loss, elite monster ambushes, hostile attack drones, lunar gravity, or temporary artifact trials.
- **Per-Player Scoping**: Hazard modifiers and event effects apply strictly to the player who triggered them, preserving fair play in multiplayer.

---

## ⚙️ Configuration & Customization

Tune gameplay mechanics on the fly via the in-game **Risk of Options** menu or the configuration file (`com.user.itemroulette.cfg`):
- **Item & Artifact Pool Management**: Fine-tune drop rates, toggle individual Lunar items, and configure the artifact pool (including custom blacklists).
- **Equipment Barrels**: Physical equipment barrels remain untouched in stages to preserve intentional active equipment builds.
- **Multiplayer Support**: Full multiplayer compatibility with host-authoritative netcode. Fallen players in spectator/drone mode can continue spinning rolls for the next stage.
- **Third-Party Mod Compatibility**: Built-in soft-dependency support for ZetAspects, Goorakh-Quality (Quality tiers, Quality chests, Chaos scaling & custom events), modded items, and third-party artifacts.
- **Debug & Admin Menu `[F7]`**: Instantly test Chaos events, simulate rolls, and verify mechanics.

---

## ⌨️ Default Controls

| Key | Action |
| :--- | :--- |
| **`[B]`** | Spin Item Reel / Accept Void Item |
| **`[R]`** | Reroll Void Item (in decision prompt) |
| **`[C]`** | Decline Void Corruption / Void Reel Menu |
| **`[V]`** *(hold 1s)* | Toggle Auto-Spin |
| **`[K]`** | Toggle Casino Roulette Wheel |
| **`[F7]`** | Open Debug & Chaos Testing Menu |
| **`[ESC]`** | Close Open Mod Panels |

*All hotkeys can be rebound under the `01. Controls` configuration section.*

---

## 📦 Installation

1. Install [BepInExPack](https://thunderstore.io/package/bbepis/BepInExPack/) for Risk of Rain 2.
2. *(Recommended)* Install [Risk of Options](https://thunderstore.io/package/Rune580/RiskOfOptions/) for in-game configuration.
3. Extract the mod archive into your `Risk of Rain 2/BepInEx/plugins/ItemRoulette/` directory.

---

## 💬 Feedback & Bug Reporting

Found a bug, want to suggest new Chaos Events, or discuss balance? Join our community:

- **Discord**: [Join our Discord Server](https://discord.gg/mpHBdhMBVY) (`https://discord.gg/mpHBdhMBVY`)
- When reporting bugs, please include your BepInEx log file (`BepInEx/LogOutput.log`) and a brief description of what happened.

---

## 💖 Special Thanks / Credits

A heartfelt special thanks for invaluable assistance and support:
- **Videowiz92**

