## Context

The PolygonMerge library has 100% code coverage and is ready for public consumption. It needs to be published as a NuGet package via a public GitHub repository with automated CI/CD. The reference implementation at `approximate-span-matching` provides the publish workflow pattern using NuGet Trusted Publishing (OIDC). We adapt that pattern and add a CI workflow with coverage enforcement.

## Goals / Non-Goals

**Goals:**
- Public GitHub repository hosting the PolygonMerge source.
- CI workflow on every push and PR: build, test, coverage check (100% threshold).
- CD workflow on `v*` tags: build, pack, publish to NuGet.org via Trusted Publishing.
- Zero long-lived secrets — use OIDC for NuGet authentication.

**Non-Goals:**
- Versioning strategy beyond tag-based (manual tag → publish).
- Multi-target framework publishing.
- Release notes generation.
- Chocolatey or other package feeds.

## Decisions

### Decision 1: Two Workflow Files (CI + CD)

**Choice:** Separate `build.yml` (CI) and `publish.yml` (CD) rather than a single combined workflow.

**Rationale:** CI runs on every push/PR and validates correctness. CD triggers only on version tags and publishes. Separating them keeps the CI fast (no packing/publishing overhead) and makes the CD trigger explicit (tag = intent to release).

**Alternatives considered:**
- Single workflow with conditional jobs: More complex, harder to read, and the conditional logic for "publish only on tag" can be brittle.

### Decision 2: Trusted Publishing (OIDC) Instead of API Keys

**Choice:** Use `NuGet/login@v1` with `id-token: write` permission to obtain a short-lived NuGet API key via OIDC token exchange.

**Rationale:** No long-lived API key to store, rotate, or leak. The OIDC token is automatically issued by GitHub Actions and cryptographically signed. The nuget.org trusted publishing policy ties the token to a specific repo + workflow + optional environment.

**Prerequisites:** A trusted publishing policy must be configured on nuget.org matching `Repository Owner` + `Repository` + `Workflow File` (`publish.yml`).

### Decision 3: Version from Git Tag

**Choice:** Extract the version from the git tag (`v1.2.3` → `1.2.3`) and pass it to `dotnet build` and `dotnet pack` via `-p:Version`.

**Rationale:** The tag is the single source of truth for the release version. No need to maintain version in `.csproj` or a separate file. This matches the reference implementation at `approximate-span-matching`.

### Decision 4: Ubuntu Runner

**Choice:** Use `ubuntu-latest` for both CI and CD workflows.

**Rationale:** .NET 10 is fully cross-platform. Ubuntu runners are faster and cheaper (free for public repos). No Windows-specific dependencies exist in the library.

## Risks / Trade-offs

- **[Risk] First publish must succeed to permanently activate the trusted publishing policy.** The policy starts in a temporary 7-day window. → **Mitigation:** Test the build/pack steps locally first. The first tag push should be a pre-release version (e.g., `v1.0.0-preview1`) to validate the pipeline.
- **[Risk] NUGET_USERNAME secret must be set in GitHub.** → **Mitigation:** This is a one-time setup per repo. The value is the nuget.org username (profile name, not email).
