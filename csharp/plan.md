# Copilot-only cutover plan

## Goal
Remove Anthropic as a provider and make the application operate as a Copilot-powered coding agent without losing current functionality.

## Phase 1: Provider and client cutover
- Replace the generic client abstraction name so it no longer references Anthropic.
- Remove Anthropic provider branching from startup and config resolution.
- Normalize defaults, config, and doctor output to Copilot-only behavior.
- Remove the Anthropic service implementation and its references.

## Phase 2: Copilot-only product cleanup
- Remove Anthropic-specific constants, environment variables, and model defaults.
- Update CLI/provider text to reflect Copilot-only operation.
- Keep existing tooling and agent loop behavior intact.

## Phase 3: Agent capability hardening
- [x] Preserve richer turn history when tools are used.
- [x] Expand coding-agent tool orchestration where Copilot requires additional nudging.
- [x] Continue validating build and interactive scenarios.

## Phase 4: Editing workflow hardening
- [x] Add a structured patch tool for multi-edit code changes.
- [ ] Strengthen build/test reflection loops after edits.
- [ ] Improve edit verification and recovery when tool-driven changes fail.
