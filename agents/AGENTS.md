# Agents Guide

Shared notes for AI/automation working on the Cards Godot project.

## Project Context
- READ `README.md` for run instructions and controls
- READ `agents/PROJECT.md` for structure and conventions

## Common Tasks
- Run the game via Godot 4.4 .NET; main scene is `res://AzurLane/scenes/main.tscn`
- Test scenes: `res://AzurLane/tests/Tests.tscn`, `res://tests/shader.tscn`

## Notes
- This is a Godot 4.4 .NET (C#) project; avoid Node/npm assumptions
- Avoid removing user changes; never reset the repo without explicit instruction

## Code Standards
- Do not write fallbacks unless told to, throw Errors with the reason of the failure
- Always look for typeguards to check a type
- Don't do inline type coercion
- Don't try to create flags, unless explicitly asked to
- Try to reuse code as much as possible

## Git
- Avoid commiting or pushing without user permission
- Commit message must have a title and a description with min 50 words, use bullet list of changes format
- Avoid using file paths in commit messages
- In commit title, avoid "feat:" or "chore:"
- NEVER write coauthored information in the commit message
