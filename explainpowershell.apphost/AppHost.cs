var builder = DistributedApplication.CreateBuilder(args);

// Configure Azure Storage emulator (Azurite) for local development
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

// Add the Azure Table storage resource used by the analysis service
var tables = storage.AddTables("tables");

// Add the Azure Functions analysis service backend
var analysisService = builder.AddAzureFunctionsProject<Projects.explainpowershell>("analysisservice")
    .WithReference(tables)
    .WaitFor(tables);

// Add the Blazor WebAssembly frontend as a static web app
// Note: Blazor WASM runs in the browser - the appsettings.json configures the BaseAddress
// to point to the analysis service at http://localhost:7071/api/
builder.AddProject<Projects.explainpowershell_frontend>("frontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();
