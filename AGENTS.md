# AGENTS.md — HomeMatch Project Rules & AI Agent Operating Protocol

> **CRITICAL DIRECTIVE FOR ALL AI AGENTS (Cursor, Copilot, Claude, Windsurf, Antigravity, Aider, etc.):**
>
> You are operating within the **HomeMatch** repository. You MUST read, understand, and strictly follow every rule defined in this document.
>
> ⚠️ **MANDATORY FIRST-TURN ACKNOWLEDGMENT PROTOCOL:**
> In your **very first response** to any task or user prompt, before creating or modifying any code, you **MUST** output the following compliance block:
>
> ```markdown
> ### 🛡️ AGENTS.md Compliance Check
> - **Task Scope:** [Brief description of the assigned task and boundaries]
> - **Target Module/Files:** [List specific directories/files to be touched]
> - **Architecture Docs Consulted:** [e.g., modular-monolith.md, schema-design.md, api-guidelines.md]
> - **Proposed Plan:** [Step-by-step implementation plan]
> - **Status:** ⏸️ Awaiting human approval before touching any code.
> ```
>
> **DO NOT modify or create any code until the user explicitly approves the implementation plan.**

---

## 1. Project Context & Architecture

HomeMatch is a Monorepo comprising a Next.js 15 Frontend and an ASP.NET Core .NET 10 Modular Monolith Backend with PostgreSQL 16 + PostGIS.

### 1.1 Repository Structure
- `frontend/`: Next.js 15 (App Router), TypeScript, Tailwind CSS.
  - Business features are isolated in `src/features/` (`auth`, `properties`, `listings`, `favorites`, `viewings`, `messaging`, `leasing`, `billing`, `notifications`).
  - Shared components in `src/components/`, common logic in `src/lib/`, `src/hooks/`, `src/types/`.
- `backend/`: ASP.NET Core (.NET 10) Modular Monolith.
  - `src/HomeMatch.Api/`: Entry point, routing, middleware, Swagger/Scalar docs.
  - `src/HomeMatch.Shared/`: Shared contracts, common abstractions (`IModule`, `IDomainEvent`), and API response types (`ApiResponse<T>`, `Result<T>`).
  - `src/Modules/`: 8 independent business modules (`Identity`, `Properties`, `Listings`, `Viewings`, `Messaging`, `Leasing`, `Billing`, `Notifications`).
  - `tests/`: `UnitTests/` and `IntegrationTests/`.
- `infrastructure/`: Docker Compose (`compose.yaml`), Nginx reverse proxy configuration, and PostgreSQL initialization scripts.
- `docs/`: Technical specifications.

### 1.2 Mandatory Architectural Documents to Read Before Coding
Agents MUST inspect and adhere to the relevant documentation in `docs/`:
- **Architecture:** `docs/architecture/modular-monolith.md`
  - *Strict Rule:* Modules are completely isolated. **Never** reference another module's `DbContext` or internal entities directly. Cross-module communication must go through `HomeMatch.Shared/Contracts` or asynchronous In-Memory / Outbox events.
- **Database:** `docs/database/schema-design.md`
  - *Strict Rule:* Single PostgreSQL database with dedicated schemas per module (`identity`, `properties`, `listings`, etc.). Spatial queries must use PostGIS (`geography(Point, 4326)` with GiST index). Media must be stored on Cloudflare R2 (DB stores URLs only).
- **API Guidelines:** `docs/api/api-guidelines.md`
  - *Strict Rule:* RESTful standard (`/api/v1/<resource>`). Responses must wrap data in `ApiResponse<T>`. Follow standard HTTP status codes (`200`, `201`, `204`, `400`, `401`, `403`, `404`, `500`).

---

## 2. Task Scope & Boundaries

- **Single Scope Focus:** Only modify files within the assigned task scope (e.g., if working on the `Listings` module, do not touch `Identity` or `Billing`).
- **No Unrelated Features:** Do not implement features outside the user's explicit prompt.
- **No Unsolicited Refactoring:** Do not clean up, rename, or reformat files unrelated to the task.
- **Dependency Guardrail:** Do not install new npm packages, NuGet packages, or external libraries without explicit user confirmation.

---

## 3. Before Coding (Workflow Gate)

Every agent MUST execute the following sequence before writing any implementation code:

1. **Understand Task:** Read and fully understand the assigned task requirements and acceptance criteria.
2. **Review Documentation:** Read the relevant project documentation in `docs/` (`modular-monolith.md`, `schema-design.md`, `api-guidelines.md`).
3. **Inspect Existing Code:** Explore existing patterns in the target module or feature to ensure consistency.
4. **Identify Affected Files:** Determine exact files that need to be created, modified, or deleted.
5. **Propose Implementation Plan:** Present a clear, step-by-step implementation plan to the user.
6. **Hard Gate:** **Do not modify any code until the user approves the implementation plan.**

---

## 4. During Coding Rules

- **Minimal Change Principle:** Make only the minimum changes necessary to satisfy the assigned task.
- **Preserve Public Interfaces:** Keep existing public APIs, contracts, and method signatures intact unless a breaking change has been explicitly approved.
- **Strict Clean Architecture:** Respect module layering (`Domain` -> `Application` -> `Infrastructure` -> `Presentation`). Domain layers must remain free of external dependencies.
- **Testing:** Add or update unit tests in `backend/tests/UnitTests/` or integration tests in `backend/tests/IntegrationTests/` for any new business logic.
- **No Overwriting:** Do not overwrite, remove, or discard comments, docstrings, or code written by team members without explicit instruction.
- **Conflict Stop:** If a merge conflict, scope conflict, or design clash is detected, stop immediately and report it to the user.

---

## 5. Git & Branching Rules

- **Never Commit/Push Directly to `main`:** All work must be conducted on an isolated feature branch.
- **Branch Naming Convention:** `feature/<module-name>-<task-name>` (e.g., `feature/listings-search-filter`, `feature/identity-jwt-login`).
- **Commit Message Rules (Mandatory for Agent commits & pushes):**
  - **Unaccented Vietnamese Subject Line:** The commit message subject MUST be written in unaccented Vietnamese (tiếng Việt không dấu) without diacritics or tone marks (e.g., `them api dat lich xem phong`, `sua loi xac thuc token`, `cap nhat giao dien trang chu`).
  - **Single-Line Title Only:** Strictly **DO NOT** write detailed commit bodies, descriptions, or bullet points. Only use a single-line title command: `git commit -m "<unaccented vietnamese title>"`.
- **Pushing via Agent:** When an Agent commits and pushes to GitHub, it must verify that it is on the assigned feature branch and strictly adheres to the single-line unaccented Vietnamese commit message rule above.
- **No History Rewrites:** Never force push (`--force`, `-f`) or rewrite history on shared branches.
- **No Auto-Merge:** Never automatically merge Pull Requests.
- **Protect Shared Configs:** Do not modify shared infrastructure or CI files (`compose.yaml`, `.github/workflows/*`, `nginx.conf`, root `.env.example`, `.sln` configuration) without prior approval.

---

## 6. Definition of Done (DoD)

A task is considered complete ONLY when all of the following conditions are met:

1. All acceptance criteria specified by the user are fully satisfied.
2. All relevant unit and integration tests pass successfully (`dotnet test backend/HomeMatch.sln`).
3. Build and lint checks pass without errors:
   - Backend: `dotnet build backend/HomeMatch.sln`
   - Frontend: `cd frontend && npm run build`
4. No files outside the assigned scope were modified.
5. All changes are clearly documented and summarized.
6. The code is ready for human Code Review.

---

## 7. Stop Conditions (When the Agent Must Halt)

The AI Agent must **immediately halt execution and ask the user for approval** when:

- The task requirements, business logic, or acceptance criteria are ambiguous or contradictory.
- System architectural changes are required (e.g., adding a new module, changing cross-module communications).
- Changes to database schemas, migrations, or tables of another module are needed.
- Modifications to shared contracts (`HomeMatch.Shared/Contracts`) or breaking changes to existing APIs are needed.
- Proposed changes may break existing functionality or test suites.

> 🚫 **The AI Agent is strictly forbidden from making independent assumptions or decisions in the above scenarios.**

---

## 8. Final Report Template

Upon completing any task, the AI Agent must provide a final closing report formatted as follows:

```markdown
### 📋 Task Completion Report

#### 1. Implemented Features
- [Summary of completed functionality and acceptance criteria satisfied]

#### 2. Modified Files
- `[path/to/created_file]` (Created)
- `[path/to/modified_file]` (Modified)
- `[path/to/deleted_file]` (Deleted)

#### 3. Test & Verification Results
- **Build Status:** [Passed / Not run - specify command]
- **Unit/Integration Tests:** [Passed / Not run - specify command]
- **Lint/Type Check:** [Passed / Not run - specify command]

#### 4. Risks & Limitations
- [Identify any known limitations, edge cases, technical debt, or dependencies]

#### 5. Deviations from Approved Plan
- [Report any differences from the originally approved implementation plan, or state "None"]
```
