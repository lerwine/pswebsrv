Param(
    [Parameter(Position = 0)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [Parameter(Position = 1)]
    [ValidateSet('Test', 'Deploy', 'None')]
    [string]$Action = 'None'
)

$MsBuildPath = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name 'InstallPath').InstallPath | Join-Path -ChildPath 'msbuild.exe';

. $MsBuildPath "/property:GenerateFullPaths=true", "/property:Configuration=$Configuration", "/property:Platform=Any CPU", "/t:build", ($PSScriptRoot | Join-Path -ChildPath 'PSWebSrv.sln');

if ($LASTEXITCODE -ne 0) { return }

switch ($Action) {
    'Test' {
        . ($PSScriptRoot | Join-Path -ChildPath 'Test.ps1');
        break;
    }
}