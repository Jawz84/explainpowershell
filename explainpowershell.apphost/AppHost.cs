var builder = DistributedApplication.CreateBuilder(args);

// Use the VS Code Azurite extension (no Docker required)
// Start Azurite via VS Code Command Palette: "Azurite: Start Table Service"
// Connection string matches the default Azurite development storage settings
var storage = builder.AddConnectionString("AzureWebJobsStorage");

// Add the Azure Functions analysis service backend
var analysisService = builder.AddAzureFunctionsProject<Projects.explainpowershell>("analysisservice")
    .WithReference(storage);

// Add the Blazor WebAssembly frontend as a static web app
// Note: Blazor WASM runs in the browser - the appsettings.json configures the BaseAddress
// to point to the analysis service at http://localhost:7071/api/
builder.AddProject<Projects.explainpowershell_frontend>("frontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();
