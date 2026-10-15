# Lucid Horizons / DreamCatcher Agent Guide

Also follow `H:\Dev\RimWorld\AGENTS.md`.

This repository folder is Lucid Horizons; `About\About.xml` is authoritative
for its current DreamCatcher identity and supported RimWorld versions. Do not
treat it as a current suite member or port it incidentally.

Source lives under `Source\Dreams`; XML, languages, and textures remain local.
Do not introduce Spine or modernize the legacy build layout without an explicit
compatibility/migration task.

Inspect `Dreams.sln`, `Source\Dreams\Dreams.csproj`, package metadata, and Git
state. Resolve the requested historical version from the shared manifest and
use `rwa start -Version <manifest-id>` for live work. Verify definitions,
save/load, jobs, and translations in that version; compilation against another
version is not support evidence.
