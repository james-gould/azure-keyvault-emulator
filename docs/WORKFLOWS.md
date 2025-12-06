# GitHub Actions Workflows

This document describes the CI/CD workflow chain for the Azure Key Vault Emulator project.

## Workflow Chain Overview

The project uses a chained workflow approach to ensure quality gates before publishing releases:

```
┌─────────────────────┐
│  Build and Test     │  (Manual trigger)
│  ✓ Restore          │
│  ✓ Build (Release)  │
│  ✓ Run Tests        │
└──────────┬──────────┘
           │
           │ On Success
           │
           ├─────────────────────────────────┐
           │                                 │
           ▼                                 ▼
┌──────────────────────────┐    ┌──────────────────────────┐
│ Publish NuGet & Docker   │    │ Publish ARM Docker       │
│ ✓ Fetch Git Tag          │    │ ✓ Fetch Git Tag          │
│ ✓ Build (Release)        │    │ ✓ Build Docker (ARM64)   │
│ ✓ Pack NuGet             │    │ ✓ Push to Registry       │
│ ✓ Publish to NuGet       │    └──────────────────────────┘
│ ✓ Build Docker (amd64)   │
│ ✓ Push to Registry       │
└──────────────────────────┘
```

## Workflows

### 1. Build and Test (`build-and-test.yml`)

**Trigger**: Manual (`workflow_dispatch`)

**Purpose**: Validates the codebase passes all acceptance tests.

**Steps**:
- Checkout repository
- Setup .NET (8.0.x, 9.0.x, 10.0.x)
- Install SSL certificates for testing
- Restore dependencies
- Build in Release configuration
- Run API Integration Tests
- Run TestContainers Integration Tests

**When to use**: Run this workflow manually before creating a new release. It ensures all tests pass before triggering the publishing workflows.

### 2. Publish NuGet Packages and Docker Images (`publish-all-manual.yml`)

**Trigger**: Automatic on successful completion of "Build and Test" workflow (on `master` branch)

**Purpose**: Builds and publishes NuGet packages and x64 Docker images under the latest git tag.

**Steps**:
- Checkout repository
- Fetch all git tags
- Detect latest version tag (v*.*.*)
- Setup .NET
- Restore dependencies
- Build in Release configuration
- Pack NuGet packages
- Publish to NuGet.org
- Build Docker image (latest and versioned tags)
- Push to container registry

**Prerequisites**: A version tag (e.g., `v1.0.0`) must exist in the repository.

### 3. Publish ARM Docker Images (`publish-all-arm-manual.yml`)

**Trigger**: Automatic on successful completion of "Build and Test" workflow (on `master` branch)

**Purpose**: Builds and publishes ARM64 Docker images under the latest git tag.

**Steps**:
- Checkout repository
- Fetch all git tags
- Detect latest version tag (v*.*.*)
- Setup Docker Buildx
- Start Docker service
- Log in to container registry
- Build ARM64 Docker image (latest-arm and versioned-arm tags)
- Push to container registry

**Prerequisites**: A version tag (e.g., `v1.0.0`) must exist in the repository.

## Release Process

To create a new release:

1. **Create and push a version tag**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Trigger the Build and Test workflow**:
   - Go to Actions → Build and Test
   - Click "Run workflow"
   - Select the `master` branch
   - Click "Run workflow"

3. **Wait for tests to pass**:
   - The workflow will restore, build, and test the codebase
   - If tests fail, fix the issues and try again

4. **Automatic publishing**:
   - Once tests pass, both publish workflows will trigger automatically
   - NuGet packages and Docker images (x64 and ARM64) will be published
   - Images will be tagged with both `latest`/`latest-arm` and the version number

## Benefits

- **Single-click release**: Just trigger "Build and Test" manually
- **Quality gates**: Tests must pass before publishing
- **Faster workflows**: No repeated test runs across publishing workflows
- **Reduced manual intervention**: Publishing happens automatically after tests pass
- **Clear separation of concerns**: Testing, NuGet publishing, and Docker publishing are independent
