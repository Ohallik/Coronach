# Scripts waiting for their chapters

Every `.yarn` directly under `docs/writing/` is copied into the game by the dialogue step of any builder, and the
dialogue tests then require a portrait set for every speaker and a QuestDef for every quest those scripts name. A
chapter's script waits here, with its manifest, until that chapter's people and quests are defined; then both move
up one folder. `barks.json` lives in the parent folder for every chapter: a bark with no caller is harmless.
