## Background

This all started from a very specific, very annoying problem: whenever Civil3D was open and I tried to build the project in Visual Studio, the build would fail because the output DLL was locked by the running Civil3D process. Every code change meant closing Civil3D, rebuilding, and reopening it just to test one small tweak.

What follows is the result of working through that problem properly — not just patching around the file lock, but rethinking the whole setup so that testing and deployment could both be a little more comfortable. The Core/Shell split and the workflow described below are the outcome of that effort.

## Acknowledgments

Huge thanks to **chuongmep**, the author of [CadAddinManager](https://github.com/chuongmep/CadAddinManager), for building a tool that fundamentally changed how fast we could develop this project.

The biggest time sink in Civil3D/AutoCAD add-in development used to be the endless loop of "tweak the code → close the program → reopen → test again." CadAddinManager eliminated that loop entirely. For a team like ours, which relies almost entirely on AI to write code rather than writing it ourselves, this meant we could finally iterate at a pace close to that of an actual developer: "ask the AI → test immediately → ask again." The fact that this is open source and freely available made it possible for a team with no deep .NET background to use it without any real barrier to entry. Thank you again for that.

Thanks also to the same author's RevitAddInManager and NavisAddInManager, which let us keep a consistent development experience across all three platforms (Civil3D, Revit, and Navisworks).

---

## What We Learned: Writing Efficient Test Code

Getting the most out of CadAddinManager taught us that code needs to be written with "testability" in mind.

**1. A command should do one complete thing and leave nothing behind**
Commands that run and finish without leaving any trace are the ones that reliably get reloaded every time. If a command registers itself with a long-lived event (`Idle`, `SystemVariableChanged`, etc.), that assembly will stay pinned in memory for the entire session, and the file will stay locked.

**2. Classes carrying `[CommandMethod]` should always be regular classes, never `static`**
A `static class` has no constructor, and CadAddinManager's metadata reader can fail when it expects one. Plain classes with a normal constructor are the safe choice.

**3. Separate the ribbon/UI shell from the actual logic**
If the code that builds the ribbon (`IExtensionApplication`) lives in the same assembly as the actual command logic (`[CommandMethod]`), testing a single command drags the entire ribbon along with it. Splitting these into separate projects from the start means the logic-only assembly can be loaded into CadAddinManager on its own, lightweight and isolated.

**4. Every Civil3D reference should be `Private=false`**
Otherwise the heavy Autodesk assemblies get copied into the output folder and collide with the copies Civil3D has already loaded into the process.

---

## What We Learned: Writing Deployment-Ready Code

We also learned that the structure that's best for testing isn't quite the same as the structure that's best for the people who actually use the add-in.

**1. The ribbon isn't removed — it just moves**
During development we run commands directly for quick testing, but end users are far better served by clicking a ribbon button. So we kept a separate project (the "Shell") responsible for the ribbon, and at deployment time both the logic assembly ("Core") and the Shell assembly ship together in the same folder.

**2. Minimize compile-time dependencies**
By having the Shell load Core at runtime via `Assembly.LoadFrom` instead of a compile-time `ProjectReference`, Core can change constantly without ever requiring the Shell to be rebuilt. Ribbon buttons look up command classes by name (reflection) rather than referencing them directly, which lets the two projects evolve almost completely independently.

**3. Fast, restart-free iteration and stable deployment aren't in tension**
We went in assuming we'd have to trade one for the other. It turned out that once you split things correctly, stable deployment falls out naturally from the same structure that makes fast iteration possible.

---

## Usage

### During development (fast iteration, no restart)

1. Build the **Core** project only (`SmartInfraDevJH.Core.dll`) — this is the assembly with all the `[CommandMethod]` logic and no ribbon.
2. Open CadAddinManager inside Civil3D and **Add** `SmartInfraDevJH.Core.dll`.
3. Double-click any command in the tree to run it.
4. Change the code, rebuild, and use **Faceless** to re-run without touching Civil3D or CadAddinManager again.
5. Civil3D itself never needs to be closed during this loop.

### Deploying for actual use (ribbon UI)

1. Build both projects: `SmartInfraDevJH.Core.dll` (logic) and `SmartInfraDevJH.dll` (Shell, ribbon).
2. **Copy both DLLs into the same folder.** This is required — the Shell loads Core at runtime from its own folder, so they have to sit side by side.
3. Register only the Shell assembly (`SmartInfraDevJH.dll`) as the add-in to auto-load with Civil3D (via bundle/manifest, or NETLOAD for a quick check).
4. On startup, the Shell automatically loads Core from the same folder, builds the ribbon, and wires the buttons to the commands inside Core.
5. Users just click the ribbon buttons as usual — they never need to know Core and Shell are separate files.

---

Thank you again to CadAddinManager and its author for making all of this possible.
