<!--
Sync Impact Report:
Version change: 0.1.0 → 1.0.0
Modified principles: (template placeholders replaced with concrete set)
Added sections: "Architecture & Technology Stack", "Workflow & Quality Gates"
Removed sections: None (template placeholders consumed)
Templates requiring updates:
	.specify/templates/plan-template.md (✅ aligns; add performance + deterministic gates)
	.specify/templates/spec-template.md (✅ aligns; user scenarios remain independently testable)
	.specify/templates/tasks-template.md (✅ aligns; add mandatory test-first note and performance tasks)
Deferred TODOs: RATIFICATION_DATE (original adoption date unknown → TODO(RATIFICATION_DATE): needs project owner confirmation)
-->

# Bitcoin Cost Basis AI Agent System Constitution

## Core Principles

### 1. Deterministic Functional Core (NON-NEGOTIABLE)
Code that computes cost basis, tax allocations, FIFO matching, and historical price transforms MUST be pure F# functions (no side effects, deterministic given identical inputs). Side effects (I/O, network, file, database, agent calls) MUST be isolated in boundary modules. Rationale: Determinism enables reproducible tax calculations, auditability, and simpler property-based and snapshot testing.

### 2. Clean Architecture Boundaries
Layers MUST be enforced: Domain (pure functions, types) → Application (orchestration, agents) → Infrastructure (storage, external services, MCP tool adapters, container concerns). No inward dependencies on infrastructure; domain types MUST NOT reference agent framework or container APIs. Rationale: Prevents tax logic from being coupled to delivery mechanisms, enabling independent verification and future platform changes.

### 3. Test-Driven Development & Coverage Standards (NON-NEGOTIABLE)
Tests MUST be authored before implementing domain logic (Red-Green-Refactor). Minimum coverage: 95% line and 100% of public domain functions. Each cost basis scenario (FIFO partial fill, multi-buy aggregation, fee handling, date boundary, leap year, same-day buy/sell) MUST have unit tests. Integration tests MUST validate agent orchestration and MCP historical price retrieval. Property-based tests SHOULD cover numerical invariants (sum of matched lots equals sold amount). Rationale: Ensures correctness for regulatory compliance and reduces regression risk.

### 4. User Experience Consistency & Clarity
All agent/user outputs (console, file, JSON) MUST use a consistent schema: status, inputs summarized, computations, advisory notes, and next actions. Error messages MUST be actionable (include failing validation rule or missing field). UX flows MUST support incremental data entry and independent recalculation for any transaction subset. Rationale: Transparent, consistent outputs reduce user confusion and support audit trails for tax authorities.

### 5. Performance & Deterministic Scaling
Cost basis computation MUST handle at least 100k transactions with p95 < 500ms for pure computation (excluding I/O) on a modern laptop (4 cores). Memory usage SHOULD remain < 500MB for that load. Historical price lookup MUST be batched and cached (in-memory + optional persistent) to avoid redundant external fetches. Algorithms MUST be O(n log n) or better for matching; naive quadratic lot matching is forbidden. Rationale: High performance ensures timely user feedback during tax season and supports large DCA histories.

### 6. Regulatory & Data Integrity Compliance
Every computed result MUST retain provenance: source transaction IDs, matched lot IDs, historical price source timestamp, and calculation version. Fee handling MUST always be included in adjusted basis; missing fees marked explicitly. All persisted data MUST be immutable (append-only) with derivations stored separately. Rationale: Audit readiness and traceability for tax regulation scrutiny.

Use UTC for all date/time representations to avoid timezone-related discrepancies in transaction timestamps. Rationale: Ensures consistency across different user locales and prevents errors in date-based calculations.

### 7. Observability & Traceable Logging
Structured logs (JSON) MUST include correlation IDs per orchestration run, computation duration, cache hit rates, and version. No sensitive personal data logged (only hashed transaction identifiers if needed). A debug trace mode MAY emit intermediate FIFO matching steps; default mode MUST remain concise. Rationale: Enables performance tuning, debugging, and compliance auditing without leaking sensitive info.

### 8. Versioning & Backward Compatibility
Domain calculation logic version MUST increment: MAJOR for algorithmic rule changes (e.g., lot matching methodology), MINOR for new advisory fields or optional data, PATCH for clarifications/refactors without output structure change. Historical recalculations MUST specify which domain version produced them. Rationale: Ensures reproducibility of past filings.

### 9. Simplicity & Explicitness
Avoid premature abstractions. Each module MUST have a single clear responsibility (e.g., LotMatching.fs, TaxComputation.fs, HistoricalPriceAdapter.fs). Generic frameworks or reflection-based magic are prohibited in domain code. Rationale: Simplicity reduces cognitive load and increases auditability.

## Architecture & Technology Stack

Language: F# on .NET 10. Agents: Microsoft Agent Framework. Containers: Podman for local isolation and reproducible builds. MCP tools provide historical Bitcoin prices. All deterministic logic resides in pure F# modules; agent framework integration resides in orchestration layer. Docker/Podman images MUST be reproducible via a pinned .NET base image and locked dependencies. Randomness (if any for simulation) MUST be seeded. MCP (Model Context Protocol) tools will be created and used.

## Workflow & Quality Gates

Pull Requests MUST show: (1) All new domain functions accompanied by failing tests prior to implementation (verified by commit history or test diff), (2) Performance benchmarks for affected algorithms when complexity risk exists, (3) No decrease in coverage thresholds, (4) Updated version tags when output contracts change, (5) UX schema validation passing. CI MUST run: unit tests, property tests, integration tests, performance smoke (10k transactions), lint/style, container build. Merge blocked if any gate fails. Complexity exceptions REQUIRE a documented justification section added to the PR referencing Principle 9.

## Governance

This constitution supersedes conflicting legacy practices. Amendments REQUIRE: written rationale, impact assessment (performance, compliance, UX), version bump classification (MAJOR/MINOR/PATCH) with justification, migration or recalculation guidance if outputs change, and update to affected templates. Reviewers MUST verify principle adherence (checklist included in PR template). Quarterly compliance review MUST evaluate: determinism, performance benchmarks, coverage metrics, audit trail completeness. Emergency changes (critical tax rule updates) MAY bypass performance benchmark but MUST schedule a follow-up benchmark within 7 days.

**Version**: 1.0.1 | **Ratified**: 2025-10-20 | **Last Amended**: 2025-10-20

