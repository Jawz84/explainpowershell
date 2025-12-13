# Copilot instructions for Explain PowerShell

These instructions are for GitHub Copilot Chat/Edits when working in this repository.

## Repo overview (what this project is)
- A PowerShell oneliner explainer.
- Backend: Azure Functions (.NET) in `explainpowershell.analysisservice/`.
- Frontend: Blazor in `explainpowershell.frontend/`.
- Shared models: `explainpowershell.models/`.
- Tests: Pester tests in `explainpowershell.analysisservice.tests/`.
- Infra: Bicep in `explainpowershell.azureinfra/`.

## Architecture & flow
- The primary explanation is AST-based:
  - `SyntaxAnalyzer` parses PowerShell into an AST and produces a list of `Explanation` nodes.
- The AI feature is additive and optional:
  - The AST analysis returns immediately; the AI call is a separate endpoint invoked after the tree is available.
  - Frontend triggers AI in the background so the UI remains responsive.

## Editing guidelines (preferred behavior)
- Prefer small, surgical changes; avoid unrelated refactors.
- Preserve existing public APIs and JSON shapes unless explicitly requested.
- Keep AI functionality optional and non-blocking.
  - If AI configuration is missing, the app should still work (AI can silently no-op).
- Use existing patterns in the codebase (logging, DI, options, error handling).
- Don’t add new external dependencies unless necessary and justified.

## C# conventions
- Prefer async/await end-to-end.
- Handle nullability deliberately; avoid introducing new nullable warnings.
- Use `System.Text.Json` where the project already does; don’t mix serializers in the same flow unless required.

## Unit tests
- Aim for high coverage on new features.
- Focus on behavior verification over implementation details.
- When adding tests, follow existing patterns in `explainpowershell.analysisservice.tests/`.

## Building
- On fresh clones, run all code generators before building: `Get-ChildItem -Path $PSScriptRoot/explainpowershell.analysisservice/ -Recurse -Filter *_code_generator.ps1 | ForEach-Object { & $_.FullName }`

## PowerShell / Pester conventions
- Keep tests deterministic and fast; avoid relying on external services unless explicitly an integration test.
- When adding tests, follow the existing Pester structure and naming.
- Before adding Pester tests, consider if the behavior can be verified in C# unit tests first.

## Running locally
- For running Pester integration tests locally successfully it is necessary to run `.\bootstrap.ps1` from the repo root, it sets up the required data in Azurite, and calls code generators.
- For general debuging, running `.\bootstrap.ps1` once is also recommended. If Azurite is present and has helpldata, it is not necessary to run it again.
- You can load helper methods to test the functionapp locally by importing the following scripts in your PowerShell session:
```powershell
. C:\Users\JosKoelewijn\GitNoOneDrive\explainpowershell/explainpowershell.analysisservice.tests/Invoke-SyntaxAnalyzer.ps1
. C:\Users\JosKoelewijn\GitNoOneDrive\explainpowershell/explainpowershell.analysisservice.tests/Invoke-AiExplanation.ps1
. C:\Users\JosKoelewijn\GitNoOneDrive\explainpowershell/explainpowershell.analysisservice.tests/Get-HelpDatabaseData.ps1
. C:\Users\JosKoelewijn\GitNoOneDrive\explainpowershell/explainpowershell.analysisservice.tests/Get-MetaData.ps1
```

## How to validate changes
- Prefer the repo task: run the VS Code task named `run tests` (Pester).
- If you need a build check, use the VS Code `build` task.

## Documentation
- When adding developer-facing features, also update or add a CodeTour in `.tours/` when it improves onboarding.
