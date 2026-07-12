[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$resolvedRoot = [System.IO.Path]::GetFullPath($root)
$expectedSuffix = [System.IO.Path]::Combine('scratchpad', 'spike')
if (-not $resolvedRoot.EndsWith($expectedSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to run outside the expected scratchpad/spike directory: $resolvedRoot"
}

$artifacts = Join-Path $resolvedRoot 'artifacts'
if (Test-Path -LiteralPath $artifacts) {
    $resolvedArtifacts = (Resolve-Path -LiteralPath $artifacts).Path
    if (-not $resolvedArtifacts.StartsWith($resolvedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove artifacts outside the spike directory: $resolvedArtifacts"
    }
    Remove-Item -LiteralPath $resolvedArtifacts -Recurse -Force
}

$logs = New-Item -ItemType Directory -Path (Join-Path $artifacts 'logs') -Force
$packages = New-Item -ItemType Directory -Path (Join-Path $artifacts 'packages') -Force
$packageCache = New-Item -ItemType Directory -Path (Join-Path $artifacts 'package-cache') -Force

$generatorProject = Join-Path $root 'generator\Spike.Generator.csproj'
$packageProject = Join-Path $root 'package\Spike.Package.csproj'
$consumerProject = Join-Path $root 'consumer\Consumer.csproj'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)] [string] $SdkVersion,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $WorkingDirectory
    )

    $globalJson = Join-Path $WorkingDirectory 'global.json'
    $json = "{`n  `"sdk`": {`n    `"version`": `"$SdkVersion`",`n    `"rollForward`": `"disable`"`n  }`n}"
    Set-Content -LiteralPath $globalJson -Value $json -Encoding UTF8
    Push-Location $WorkingDirectory
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet command failed under SDK $SdkVersion with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
        Remove-Item -LiteralPath $globalJson -Force -ErrorAction SilentlyContinue
    }
}

Push-Location $root
try {
    & dotnet build $generatorProject -c Release -t:Rebuild -p:RoslynVersion=4.8.0 -p:RoslynLabel=roslyn4.8 -p:BaseIntermediateOutputPath=obj\roslyn4.8\ -p:BaseOutputPath=bin\roslyn4.8\
    if ($LASTEXITCODE -ne 0) { throw 'Roslyn 4.8 generator build failed.' }

    & dotnet build $generatorProject -c Release -t:Rebuild -p:RoslynVersion=5.6.0 -p:RoslynLabel=roslyn5.6 -p:BaseIntermediateOutputPath=obj\roslyn5.6\ -p:BaseOutputPath=bin\roslyn5.6\
    if ($LASTEXITCODE -ne 0) { throw 'Roslyn 5.6 generator build failed.' }

    & dotnet pack $packageProject -c Release -o $packages.FullName
    if ($LASTEXITCODE -ne 0) { throw 'Spike package creation failed.' }

    $matrix = @(
        @{ Sdk = '8.0.422'; Roslyn = '4.8'; ExpectedFolder = 'roslyn4.8' },
        @{ Sdk = '9.0.100'; Roslyn = '4.12'; ExpectedFolder = 'roslyn4.8' },
        @{ Sdk = '10.0.301'; Roslyn = '5.6'; ExpectedFolder = 'roslyn5.6' }
    )

    $rows = @()
    foreach ($entry in $matrix) {
        $sdkLabel = $entry.Sdk.Split('.')[0]
        $log = Join-Path $logs.FullName "build-sdk$sdkLabel.log"
        $binlog = Join-Path $logs.FullName "build-sdk$sdkLabel.binlog"
        $cache = Join-Path $packageCache.FullName "sdk$sdkLabel"

        $arguments = @(
            'build', $consumerProject,
            '-c', 'Release',
            '-t:Rebuild',
            "-p:SpikePackagesPath=$cache",
            "-flp:logfile=$log;verbosity=diagnostic",
            "-bl:$binlog"
        )
        Invoke-DotNet -SdkVersion $entry.Sdk -Arguments $arguments -WorkingDirectory (Split-Path $consumerProject)

        $expectedFragment = "$($entry.ExpectedFolder)\cs\RoyalCode.SmartSelector.Generators.dll"
        if (-not (Select-String -LiteralPath $log -SimpleMatch $expectedFragment -Quiet)) {
            throw "SDK $($entry.Sdk) did not load the expected analyzer folder $($entry.ExpectedFolder)."
        }

        $consumerDll = Join-Path (Split-Path $consumerProject) 'bin\Release\net8.0\Consumer.dll'
        $execution = & dotnet $consumerDll
        if ($LASTEXITCODE -ne 0 -or $execution -ne 'OK: 7 Spike') {
            throw "SDK $($entry.Sdk) consumer execution failed: $execution"
        }

        $rows += "| $($entry.Sdk) | $($entry.Roslyn) | ``$($entry.ExpectedFolder)`` | OK | OK (``$execution``) |"
    }

    $packagePath = Join-Path $packages.FullName 'SpikeGen.0.0.1.nupkg'
    $packageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss K'
    $summary = @(
        '# Ultima execucao do spike',
        '',
        "Executado em: $timestamp",
        '',
        '| SDK | Roslyn | Pasta selecionada | Build | Execucao |',
        '|---|---|---|---|---|',
        $rows,
        '',
        "SHA-256 de ``SpikeGen.0.0.1.nupkg``: ``$packageHash``",
        '',
        'Os logs diagnosticos e binlogs completos foram gravados em `artifacts/logs/`.'
    )
    Set-Content -LiteralPath (Join-Path $root 'last-run.md') -Value $summary -Encoding UTF8
}
finally {
    Pop-Location
}
