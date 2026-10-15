---
name: Dark Space Physics and Replay
description: Change Dark Space deterministic simulation, flight physics, command timing, snapshot hashes, and regression tests.
---

# Physics-first deterministic changes

Paths below are relative to the repository root. Read `AGENT.md` and the affected Core/tests first.

1. Identify the mode: `EliteAligned` intentionally follows the nose with a speed limit;
   `Newtonian` must satisfy the relevant physical conservation laws.
2. State the invariant and units before editing the integrator. Reuse `SystemScale` and physics helpers.
3. A replay starts from an explicit world seed/configuration and advances a fixed finite positive dt.
   Commands belong to an explicit tick and preserve enqueue order within that tick.
   Never infer authoritative tick timing from rendering FPS or wall-clock time.
4. Verify repeat runs, changed-input sensitivity, command-order sensitivity, and invalid inputs.
5. Hash a canonical snapshot representation with deterministic key ordering and invariant encoding.
   Never use `GetHashCode()` or default dictionary iteration as a persistent digest.
6. A snapshot digest is not a full save/checkpoint: hidden state and pending commands require separate handling.
7. Golden hashes must be literal, reviewed fixtures. Never regenerate expected values automatically on failure.
   If a physics change intentionally changes a golden, inspect the state difference and document why.
8. Exact floating-point reproducibility on one host does not prove cross-platform determinism.
   Keep a cross-platform check or explicitly report what has and has not been tested.
9. Validate finite values: NaN and infinity must not silently enter time integration.
10. Run unit/integration tests and update `MEMORY.md` with results and remaining limitations.
