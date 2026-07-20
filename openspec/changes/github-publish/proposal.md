## Why

The PolygonMerge library is complete and tested at 100% coverage, but exists only locally. For others to consume it as a NuGet package, it needs a public GitHub repository with automated CI (build, test, coverage) and CD (publish to NuGet.org on version tags). Trusted Publishing via OIDC eliminates the need to manage long-lived NuGet API keys.

## What Changes

- Create a public GitHub repository for the PolygonMerge project.
- Push the existing codebase (library, tests, solution, README) to GitHub.
- Add a GitHub Actions CI workflow (`build.yml`) that builds, runs tests with 100% coverage threshold, and validates on every push and PR.
- Add a GitHub Actions publish workflow (`publish.yml`) triggered by `v*` tags that builds, packs, and publishes to NuGet.org using Trusted Publishing (OIDC).
- Configure nuget.org trusted publishing policy matching the repo + workflow.

## Capabilities

### New Capabilities

- `ci-cd-pipeline`: GitHub Actions workflows for continuous integration (build + test + coverage gate on push/PR) and continuous delivery (NuGet publish on version tags via OIDC trusted publishing).

### Modified Capabilities

<!-- No existing capabilities modified — infrastructure change. -->

## Impact

- New files: `.github/workflows/build.yml`, `.github/workflows/publish.yml`
- Requires GitHub repository creation (public)
- Requires nuget.org trusted publishing policy configuration
- Requires GitHub Actions OIDC permissions (`id-token: write`)
- No code changes to the library itself
