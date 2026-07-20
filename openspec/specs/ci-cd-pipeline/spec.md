# ci-cd-pipeline Specification

## Purpose
Defines the GitHub Actions CI/CD workflows: `build.yml` (push/PR: .NET 10 build, test, 100% coverage gate) and `publish.yml` (version tags: build, pack, OIDC-based NuGet Trusted Publishing). Covers repository configuration requirements for public hosting and secure package publication.
## Requirements
### Requirement: CI Workflow on Push and Pull Request
The repository SHALL include a GitHub Actions workflow at `.github/workflows/build.yml` that triggers on every push to any branch and every pull request.

The workflow SHALL:
1. Checkout the repository.
2. Set up .NET 10 SDK.
3. Build the solution in Release configuration.
4. Run tests with code coverage collection using Coverlet.
5. Enforce 100% line and branch coverage thresholds.

#### Scenario: Push to main triggers CI
- **WHEN** a commit is pushed to any branch
- **THEN** the build workflow runs, builds the solution, runs tests, and checks coverage

#### Scenario: CI fails on coverage drop
- **WHEN** tests run and coverage falls below 100% line or branch
- **THEN** the workflow fails

### Requirement: CD Workflow on Version Tag
The repository SHALL include a GitHub Actions workflow at `.github/workflows/publish.yml` that triggers on tags matching the pattern `v*`.

The workflow SHALL:
1. Checkout the repository.
2. Set up .NET 10 SDK.
3. Extract the version from the git tag (strip `v` prefix).
4. Build the library in Release configuration with the extracted version.
5. Pack the library into a `.nupkg` file.
6. Authenticate to NuGet.org using Trusted Publishing (OIDC) via `NuGet/login@v1`.
7. Push the package to NuGet.org.

The workflow SHALL request `id-token: write` permission at the job level.

#### Scenario: Tag push triggers publish
- **WHEN** a tag matching `v*` (e.g., `v1.0.0`) is pushed
- **THEN** the publish workflow builds, packs, and pushes the package to NuGet.org

#### Scenario: Version extracted from tag
- **WHEN** the tag is `v2.1.3`
- **THEN** the package is built and packed with version `2.1.3`

### Requirement: NuGet Trusted Publishing Authentication
The publish workflow SHALL authenticate to NuGet.org using the `NuGet/login@v1` action, which exchanges a GitHub OIDC token for a short-lived NuGet API key.

The workflow SHALL pass a `user` parameter to `NuGet/login@v1` sourced from a GitHub Actions secret (`secrets.NUGET_USERNAME`).

The short-lived API key SHALL be passed to `dotnet nuget push` via the `--api-key` parameter.

#### Scenario: OIDC token exchange succeeds
- **WHEN** the publish workflow runs with a valid trusted publishing policy on nuget.org
- **THEN** a temporary API key is obtained and used to push the package

### Requirement: GitHub Repository Configuration
The repository SHALL be public on GitHub.

The repository SHALL contain a `LICENSE` file (MIT).

The repository SHALL have the `NUGET_USERNAME` secret configured in GitHub Actions settings.

#### Scenario: Public repository accessible
- **WHEN** a user navigates to the repository URL
- **THEN** the source code, README, and license are visible

