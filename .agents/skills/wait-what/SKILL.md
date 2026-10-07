---
name: wait-what
description: "Stop. That last message did not land: re-pitch it."
disable-model-invocation: true
when_to_use: "User explicitly invokes wait-what after a confusing agent message; re-pitches it in Simplified Technical English using the repo's glossary."
allowed-tools: Read
version: 1.0.0
---

Wait, I don't understand where you've got to here. Re-pitch that: give me a little bit of context, talk in ASD-STE100 Simplified Technical English, and use the ubiquitous language from `GLOSSARY.md` (follow `GLOSSARY-MAP.md` to the right one if the repo has more than one).
