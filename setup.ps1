# Script to set up ExplainPowerShell development environment on Windows
# Requires Windows Package Manager (winget)

# Check if running as administrator
# if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
#     Write-Error "Please run this script as Administrator"
#     exit 1
# }

Write-Host "Installing development dependencies..." -ForegroundColor Green

# Install PowerShell 7 if not already installed
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) {
    Write-Host "Installing PowerShell 7..."
    winget install Microsoft.PowerShell
}

# Install .NET SDK
Write-Host "Installing .NET SDK..."
winget install Microsoft.DotNet.SDK.7

# Install Azure CLI
Write-Host "Installing Azure CLI..."
winget install Microsoft.AzureCLI

# Install Git if not present
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Git..."
    winget install Git.Git
}

# Install Visual Studio Code if not present
if (-not (Get-Command code -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Visual Studio Code..."
    winget install Microsoft.VisualStudioCode
}

# Install recommended VS Code extensions
Write-Host "Installing VS Code extensions..."
code --install-extension ms-azuretools.vscode-azurefunctions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-vscode.powershell
code --install-extension ms-dotnettools.blazorwasm-companion
code --install-extension github.vscode-pull-request-github
code --install-extension github.vscode-github-actions
code --install-extension Azurite.azurite

# Restore .NET packages
Write-Host "Restoring .NET packages..."
dotnet restore

Write-Host "`nSetup complete! Please restart your terminal to ensure all changes take effect." -ForegroundColor Green
Write-Host "To start Azurite, open the Command Palette in VS Code (Ctrl+Shift+P) and run 'Azurite: Start'" -ForegroundColor Yellow