# Script de Automatización de Infraestructura Común para E-Commerce G18
# Este script propaga: Exception Handlers, Exceptions, Health Checks, Middlewares y Carpetas de Extensión
# desde Products.API a todos los demás microservicios (Users.API, Cart.API, Notifications.API, Orders.API)
# También estandariza sus archivos Program.cs al diseño modular y limpio de la cátedra.

$apis = @("Users.API", "Cart.API", "Notifications.API", "Orders.API")

# Detectamos la ruta base de forma dinámica donde se encuentra este script
$basePath = $PSScriptRoot
if ([string]::IsNullOrEmpty($basePath)) {
    $basePath = Get-Location
}

Write-Host "Iniciando propagación de infraestructura desde Products.API..." -ForegroundColor Cyan
Write-Host "Directorio Base detectado: $basePath" -ForegroundColor DarkGray

$sourceApi = "Products.API"
$sourceApiPath = Join-Path $basePath $sourceApi

# Carpetas de infraestructura que queremos propagar
$foldersToPropagate = @("ExceptionHandlers", "Exceptions", "HealthChecks", "Middleware", "Extensions")

foreach ($api in $apis) {
    Write-Host "--------------------------------------------------" -ForegroundColor Yellow
    Write-Host "Procesando microservicio: $api..." -ForegroundColor Yellow
    $apiPath = Join-Path $basePath $api

    # 1. Copiar las carpetas de infraestructura y ajustar namespaces
    foreach ($folder in $foldersToPropagate) {
        $sourceFolder = Join-Path $sourceApiPath $folder
        $targetFolder = Join-Path $apiPath $folder

        if (Test-Path $sourceFolder) {
            if (!(Test-Path $targetFolder)) {
                New-Item -ItemType Directory -Force -Path $targetFolder | Out-Null
                Write-Host "  Carpeta creada: $folder" -ForegroundColor DarkGreen
            }

            $files = Get-ChildItem -Path $sourceFolder -Filter "*.cs"
            foreach ($file in $files) {
                $targetFile = Join-Path $targetFolder $file.Name
                $content = Get-Content -Path $file.FullName -Raw

                # Reemplazamos los namespaces específicos de Products.API a los de la API destino
                $content = $content -replace "Products\.API", $api

                # Ajustes específicos para GlobalExceptionHandler.cs (Mapeo de constantes de error interno por microservicio)
                if ($file.Name -eq "GlobalExceptionHandler.cs") {
                    if ($api -eq "Users.API") {
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorCode", "Constants.UserErrors.InternalError.Code"
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorMessage", "Constants.UserErrors.InternalError.Message"
                    }
                    elseif ($api -eq "Cart.API") {
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorCode", "Constants.CartErrors.InternalError.Code"
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorMessage", "Constants.CartErrors.InternalError.Message"
                    }
                    elseif ($api -eq "Notifications.API") {
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorCode", "Constants.NotificationErrors.InternalError.Code"
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorMessage", "Constants.NotificationErrors.InternalError.Message"
                    }
                    elseif ($api -eq "Orders.API") {
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorCode", "Constants.OrderErrors.InternalError.Code"
                        $content = $content -replace "Constants\.ProductErrors\.InternalErrorMessage", "Constants.OrderErrors.InternalError.Message"
                    }
                }

                Set-Content -Path $targetFile -Value $content -Encoding UTF8
                Write-Host "    Copiado y adaptado: $folder/$($file.Name)" -ForegroundColor Green
            }
        }
    }

    # 2. Agregar los paquetes de HealthChecks, Dapper y SQLite al archivo .csproj si no están presentes
    $csprojPath = Join-Path $apiPath "$api.csproj"
    if (Test-Path $csprojPath) {
        $csprojContent = Get-Content -Path $csprojPath -Raw

        # Definimos los paquetes a inyectar
        $packagesToAdd = @(
            '<PackageReference Include="AspNetCore.HealthChecks.UI" Version="8.0.0" />',
            '<PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.0.0" />',
            '<PackageReference Include="AspNetCore.HealthChecks.UI.InMemory.Storage" Version="8.0.0" />',
            '<PackageReference Include="Dapper" Version="2.1.35" />',
            '<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.8" />'
        )

        $modified = $false
        foreach ($package in $packagesToAdd) {
            # Extraemos el nombre del paquete para buscar si ya existe
            $packageName = ""
            if ($package -match 'Include="([^"]+)"') { $packageName = $Matches[1] }

            if ($csprojContent -notmatch $packageName) {
                # Insertamos el paquete dentro del primer ItemGroup que encontremos de forma única (límite 1)
                $regex = [regex]'(<ItemGroup>)'
                $csprojContent = $regex.Replace($csprojContent, "`$1`r`n    $package", 1)
                $modified = $true
                Write-Host "  Paquete NuGet agregado al .csproj: $packageName" -ForegroundColor Cyan
            }
        }

        # Aseguramos de actualizar a la versión 8.0.11 de Microsoft.Extensions.Diagnostics.HealthChecks para evitar degradaciones
        if ($csprojContent -match 'PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks"') {
            $csprojContent = $csprojContent -replace 'PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="[^"]+"', 'PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.11"'
            $modified = $true
            Write-Host "  Actualizada versión de Microsoft.Extensions.Diagnostics.HealthChecks a 8.0.11" -ForegroundColor Cyan
        } else {
            # Si no está, lo agregamos
            $package = '<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="8.0.11" />'
            $regex = [regex]'(<ItemGroup>)'
            $csprojContent = $regex.Replace($csprojContent, "`$1`r`n    $package", 1)
            $modified = $true
            Write-Host "  Agregado Microsoft.Extensions.Diagnostics.HealthChecks a 8.0.11" -ForegroundColor Cyan
        }

        if ($modified) {
            Set-Content -Path $csprojPath -Value $csprojContent -Encoding UTF8
        }
    }

    # 3. Estandarizar Program.cs al formato minimalista
    $programPath = Join-Path $apiPath "Program.cs"
    if (Test-Path $programPath) {
        $newProgramContent = @"
using ${api}.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Inicializar Logging (Serilog)
builder.AddAppLogging();

// 1. Registro de todos los servicios (DI)
builder.Services.AddAppServices();

var app = builder.Build();

// 2. Configuración de middlewares y rutas (incluyendo Health Checks)
app.UseAppMiddleware();

app.Run();
"@
        Set-Content -Path $programPath -Value $newProgramContent -Encoding UTF8
        Write-Host "  Program.cs estandarizado al diseño modular de la cátedra." -ForegroundColor Green
    }
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "¡Propagación y estandarización completada con éxito!" -ForegroundColor Cyan
