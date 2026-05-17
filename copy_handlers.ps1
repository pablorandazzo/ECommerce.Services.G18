$apis = @("Users.API", "Cart.API", "Notifications.API")
$basePath = "c:\Users\diias\OneDrive\Escritorio\Repositorios\ECommerce.Services.G18"
$sourceHandlersPath = Join-Path $basePath "Products.API\ExceptionHandlers"
$sourceProgramPath = Join-Path $basePath "Products.API\Program.cs"

$handlers = @("ValidationExceptionHandler.cs", "NotFoundExceptionHandler.cs", "BusinessRuleExceptionHandler.cs", "GlobalExceptionHandler.cs")

foreach ($api in $apis) {
    Write-Host "Processing $api..."
    $apiPath = Join-Path $basePath $api
    $targetHandlersPath = Join-Path $apiPath "ExceptionHandlers"
    
    if (!(Test-Path $targetHandlersPath)) { New-Item -ItemType Directory -Force -Path $targetHandlersPath | Out-Null }

    foreach ($handler in $handlers) {
        $sourceFile = Join-Path $sourceHandlersPath $handler
        $targetFile = Join-Path $targetHandlersPath $handler
        
        $content = Get-Content $sourceFile -Raw
        $content = $content -replace "Products\.API\.Exceptions", "${api}.Exceptions"
        $content = $content -replace "Products\.API\.ExceptionHandlers", "${api}.ExceptionHandlers"
        
        Set-Content -Path $targetFile -Value $content -Encoding UTF8
    }

    $programPath = Join-Path $apiPath "Program.cs"
    $programContent = Get-Content $programPath -Raw

    if ($programContent -notmatch "AddProblemDetails") {
        $insertServices = @"
builder.Services.AddProblemDetails();

// Registro de Handlers en orden jerárquico (Paso a paso Persona B)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
"@
        $programContent = $programContent -replace "builder\.Services\.AddEndpointsApiExplorer\(\);", $insertServices
        
        $insertApp = @"
// Configure the HTTP request pipeline.
app.UseExceptionHandler(); // Middleware obligatorio para usar IExceptionHandler

if (app.Environment.IsDevelopment())
"@
        $programContent = $programContent -replace "// Configure the HTTP request pipeline\.\r?\nif \(app\.Environment\.IsDevelopment\(\)\)", $insertApp
        
        # If the file didn't have "// Configure the HTTP request pipeline.", try matching just the if
        if ($programContent -notmatch "app\.UseExceptionHandler\(\);") {
            $insertApp2 = "app.UseExceptionHandler();`r`n`r`nif (app.Environment.IsDevelopment())"
            $programContent = $programContent -replace "if \(app\.Environment\.IsDevelopment\(\)\)", $insertApp2
        }

        if ($programContent -notmatch "using ${api}\.ExceptionHandlers;") {
            $programContent = "using ${api}.ExceptionHandlers;`r`n" + $programContent
        }

        Set-Content -Path $programPath -Value $programContent -Encoding UTF8
        Write-Host "Updated Program.cs for $api"
    } else {
        Write-Host "Program.cs for $api already has AddProblemDetails"
    }
}
Write-Host "Done."
