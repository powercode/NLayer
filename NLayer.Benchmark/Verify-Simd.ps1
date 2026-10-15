param([string] $ScalarAssembly)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$previousAvx = $env:DOTNET_EnableAVX
$previousIntrinsics = $env:DOTNET_EnableHWIntrinsic

function Invoke-DotNet {
    param([string[]] $Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

Push-Location $repoRoot
try {
    if (-not $ScalarAssembly) {
        Invoke-DotNet @('build', 'NLayer/NLayer.csproj', '-c', 'Release', '-f', 'net10.0',
            '-p:NLayerDisableSimd=true', '-p:OutputPath=bin/scalar/')
        $ScalarAssembly = Join-Path $repoRoot 'NLayer/bin/scalar/NLayer.dll'
    }
    $ScalarAssembly = (Resolve-Path -LiteralPath $ScalarAssembly).Path
    Invoke-DotNet @('build', 'NLayer.Benchmark', '-c', 'Release', '-p:OutputPath=bin/verify/')
    $verifier = Join-Path $PSScriptRoot 'bin/verify/NLayer.Benchmark.dll'

    # Each invocation is a fresh runtime; these switches are read at startup.
    $env:DOTNET_EnableAVX = $null
    $env:DOTNET_EnableHWIntrinsic = $null
    Invoke-DotNet @($verifier, '--verify-simd', $ScalarAssembly)
    $env:DOTNET_EnableAVX = '0'
    Invoke-DotNet @($verifier, '--verify-simd', $ScalarAssembly)
    $env:DOTNET_EnableHWIntrinsic = '0'
    Invoke-DotNet @($verifier, '--verify-simd', $ScalarAssembly)
}
finally {
    $env:DOTNET_EnableAVX = $previousAvx
    $env:DOTNET_EnableHWIntrinsic = $previousIntrinsics
    Pop-Location
}
