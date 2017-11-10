Add-Type -AssemblyName 'Microsoft.Build', 'Microsoft.Build.Utilities.v4.0', 'Microsoft.Build.Framework'
$ArgTypes = @([Microsoft.Build.Framework.BuildMessageEventArgs], [Microsoft.Build.Framework.BuildErrorEventArgs], [Microsoft.Build.Framework.BuildWarningEventArgs],
    [Microsoft.Build.Framework.BuildStartedEventArgs], [Microsoft.Build.Framework.BuildFinishedEventArgs], [Microsoft.Build.Framework.ProjectStartedEventArgs],
    [Microsoft.Build.Framework.ProjectFinishedEventArgs], [Microsoft.Build.Framework.TargetStartedEventArgs], [Microsoft.Build.Framework.TargetFinishedEventArgs],
    [Microsoft.Build.Framework.TaskStartedEventArgs], [Microsoft.Build.Framework.TaskFinishedEventArgs], [Microsoft.Build.Framework.CustomBuildEventArgs],
    [Microsoft.Build.Framework.BuildStatusEventArgs], [Microsoft.Build.Framework.BuildEventArgs]);
$AllTypes = @{};
<#$ArgTypes | ForEach-Object {
    $p = @{};
    $AllTypes[$_.Name] = $p;
    $_.GetProperties() | ForEach-Object { $p[$_.Name] = $_.PropertyType.ToString() }
};
$AllProperties = $AllTypes.Keys | ForEach-Object { $c = $_; $AllTypes[$c].Keys | ForEach-Object { "$_`:$($AllTypes[$c][$_])" } } | Select-Object -Unique | Sort-Object;
[System.Windows.Clipboard]::SetText((($AllTypes.Keys | ForEach-Object {
    $k = $_;
    $_ + "`t" + (($AllProperties | ForEach-Object {
        ($n, $t) = $_.Split(':', 2);
        if ($AllTypes[$k].ContainsKey($n) -and $AllTypes[$k][$n] -eq $t) { "$t $n" } else { 'X' }
    }) -join "`t")
}) -join "`r`n"));
#>
[System.Windows.Clipboard]::SetText((($ArgTypes | ForEach-Object {
@"
            internal static LogEntry Add($($_.Name) e, LogEntry last)
            {
"@
    $_.GetProperties() | ForEach-Object { "                // $($_.PropertyType.FullName) $($_.Name)" }
@"
                throw new NotImplementedException();
            }

"@
}) -join "`r`n"));