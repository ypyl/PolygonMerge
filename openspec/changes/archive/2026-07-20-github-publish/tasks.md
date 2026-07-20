## 1. GitHub Repository Setup

- [x] 1.1 Create public GitHub repository `ypyl/PolygonMerge` via `gh repo create`
- [x] 1.2 Add `LICENSE` file (MIT) to the repository
- [x] 1.3 Add GitHub remote and push all code to the repository
- [x] 1.4 Configure `NUGET_USERNAME` secret in GitHub repository settings

## 2. CI Workflow

- [x] 2.1 Create `.github/workflows/build.yml` with push/PR trigger, .NET 10 setup, build, test with coverage
- [x] 2.2 Verify CI workflow runs successfully on push

## 3. CD Workflow

- [x] 3.1 Create `.github/workflows/publish.yml` with `v*` tag trigger, version extraction, build, pack, OIDC login, NuGet push
- [x] 3.2 Configure nuget.org trusted publishing policy for `ypyl/PolygonMerge` + `publish.yml` workflow

## 4. Validate

- [x] 4.1 Push a test tag (e.g., `v1.0.0-preview1`) and verify the publish workflow succeeds
