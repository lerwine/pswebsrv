Function Get-RegistryProperty {
    Param(
        [Parameter(Mandatory = $true)]
        [Microsoft.Win32.RegistryKey]$RegistryKey,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [AllowNull()]
        [AllowEmptyString()]
        [object]$DefaultValue,

        [int]$MaxRedirect = 32
    )

    $Value = $RegistryKey.GetValue($Name);
    if ($Value -eq $null) {
        if ($DefaultValue -ne $null) { $DefaultValue | Write-Output }
    } else {
        if ($Value -is [string]) {
            if ($Script:GetRegistryPropertyRegex -eq $null) {
                $Script:GetRegistryPropertyRegex = [System.Text.RegularExpressions.Regex]::new('^\$\(Registry:(?<p>[^\\()@]+(\\[^\\()@]+)*)@(?<n>[^\\()@]+)\)$', ([System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Compiled));
            }
            $m = $Script:GetRegistryPropertyRegex.Match($Value);
            if ($m.Success) {
                if ($MaxRedirect -gt 0) {
                    $Path = 'Microsoft.PowerShell.Core\Registry::' + $m.Groups['p'];
                    if ($Path | Test-Path) {
                        Get-RegistryProperty -RegistryKey (Get-Item -Path $Path) -Name $m.Groups['n'] -DefaultValue $DefaultValue -MaxRedirect ($MaxRedirect - 1);
                    } else {
                        if ($DefaultValue -ne $null) { $DefaultValue | Write-Output }
                    }
                } else {
                    $Value | Write-Output;
                }
            } else {
                $Value | Write-Output;
            }
        } else {
            $Value | Write-Output;
        }
    }
}

[System.Version]$Version = [System.Version]::new();
$FrameworksInstalledItems = @(@((Resolve-Path -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v*' | Get-Item) | ForEach-Object {
    if ($_.GetValue('Install', 1) -ne 1) { return }
    $i = $_;
    $Properties = @{
        Path = $_.Name;
        Key = $_.PSChildName;
        Profile = 'Full';
        Properties = @{};
    };
    $_.Property | ForEach-Object {
        if (('Version', 'InstallPath', 'Install', 'TargetVersion') -notcontains $_) {
            if ($_ -eq '(default)') {
                $Properties['Properties'][$_] = $i.GetValue('');
            } else {
                $Properties['Properties'][$_] = $i.GetValue($_);
            }
        }
    }
    $s = $_.GetValue('TargetVersion', '');
    if ($s.Length -gt 0 -and [System.Version]::TryParse($s, [ref]$Version)) { $Properties['TargetVersion'] = $Version }
    $s = Get-RegistryProperty -RegistryKey $_ -Name 'InstallPath';
    if (-not [String]::IsNullOrEmpty($s)) { $Properties['InstallPath'] = $s }
    $s = $_.GetValue('Version', '');
    
    if ($s.Length -gt 0) {
        if (-not [System.Version]::TryParse($s, [ref]$Version)) { return }
        $Properties['Version'] = $Version;
        $Properties | Write-Output;
        return;
    }
    $_ | Get-ChildItem | ForEach-Object {
        if ($_.GetValue('Install', 1) -ne 1) { return }
        $i = $_;
        $cp = @{
            Path = $_.Name;
            Key = $Properties['key'];
            Profile = $_.PSChildName;
            Properties = @{};
        };
        $Properties['Properties'].Keys | ForEach-Object {
            $cp['Properties'][$_] = $Properties['Properties'][$_];
        }
        $_.Property | ForEach-Object {
            if (('Version', 'InstallPath', 'Install', 'TargetVersion') -notcontains $_) {
                if ($_ -eq '(default)') {
                    $cp['Properties'][$_] = $i.GetValue('');
                } else {
                    $cp['Properties'][$_] = $i.GetValue($_);
                }
            }
        }
        $s = Get-RegistryProperty -RegistryKey $_ -Name 'InstallPath';
        if ([String]::IsNullOrEmpty($s)) { if ($Properties['InstallPath'] -ne $null) { $cp['InstallPath'] = $Properties['InstallPath'] } } else { $cp['InstallPath'] = $s }
        $s = $_.GetValue('Version', '');
        if ($s.Length -gt 0) {
            if (-not [System.Version]::TryParse($s, [ref]$Version)) { return }
            $cp['Version'] = $Version;
        }
        $Release = $_.GetValue('Release');
        if ($Release -ne $null) {
            switch ($Release) {
                { $_ -ge 460798 } {
                    if ($cp['Version'] -eq $null -or $cp['Version'].Major -lt 4 -or $cp['Version'].Minor -lt 7) {
                        $cp['Version'] = [System.Version]::new(4, 7);
                    }
                    break;
                }
                { $_ -ge 394802 } { $Version = [System.Version]::new(4, 6, 2); break; }
                { $_ -ge 394254 } { $Version = [System.Version]::new(4, 6, 1); break; }
                { $_ -ge 393295 } { $Version = [System.Version]::new(4, 6); break; }
                { $_ -ge 379893 } { $Version = [System.Version]::new(4, 5, 2); break; }
                { $_ -ge 378675 } { $Version = [System.Version]::new(4, 5, 1); break; }
                { $_ -ge 378389 } { $Version = [System.Version]::new(4, 5); break; }
            }
        } else {
            if ($cp['Version'] -ne $null -and $cp['Version'].Major -eq 4 -and $cp['Version'].Minor -eq 0 -and $cp['Properties']['(default)'] -eq 'deprecated') { return }
        }

        $s = $_.GetValue('TargetVersion', '');
        if ($s.Length -gt 0 -and [System.Version]::TryParse($s, [ref]$Version)) {
            $cp['TargetVersion'] = $Version;
        } else {
            if ($Properties['TargetVersion'] -ne $null) { $cp['TargetVersion'] = $Properties['TargetVersion'] }
        }
        $cp | Write-Output;
    }
}) | ForEach-Object {
    $Properties = $_;
    $Properties['Properties'] = ($Properties['Properties'].Keys | ForEach-Object {
        if ($Properties['Properties'][$_] -eq $null) {
            '$_ is null';
        } else {
            if ($_ -eq '(default)') {
                if ($Properties['Properties'][$_] -is [string]) {
                    "'$($Properties['Properties'][$_].Replace("'", "''"))'";
                } else {
                    $($Properties['Properties'][$_]).ToString()
                }
            } else {
                if ($Properties['Properties'][$_] -is [string]) {
                    "$_`: '$($Properties['Properties'][$_].Replace("'", "''"))'";
                } else {
                    "$_`: $($Properties['Properties'][$_])";
                }
            }
        }
    } | Out-String).Trim();
    New-Object -TypeName 'System.Management.Automation.PSObject' -Property $Properties;
});
$FrameworksInstalledItems | ForEach-Object {
    "$($_.Path) ($($_.Key)): $($_.Version) = $($_.InstallPath)";
};


@(Resolve-Path -Path 'HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\*' | Get-Item) | ForEach-Object {
    $MSBuildToolsPath = Get-RegistryProperty -RegistryKey $_ -Name 'MSBuildToolsPath';
    $MSBuildRuntimeVersion = $_.GetValue('MSBuildRuntimeVersion');
    if ($MSBuildRuntimeVersion -eq $null -and [System.Version]::TryParse($_.PSChildName, [ref]$Version)) {
        $MSBuildRuntimeVersion = $Version;
    }
    "$($_.Name): $MSBuildRuntimeVersion = $MSBuildToolsPath";
}

<#
$Regex = [System.Text.RegularExpressions.Regex]::new('^(?<t>[^/]+)/(?<s>[^/+]+)(\+(?<x>.+))?$', [System.Text.RegularExpressions.RegexOptions]::Compiled);
$MimeDictionary = @{};
$ExtensionDictionary = @{}
Write-Progress -Activity 'Collecting MIME info' -Status 'Reading content types' -PercentComplete 0;
$ContentTypeItems = @(Resolve-Path 'Microsoft.PowerShell.Core\Registry::HKEY_CLASSES_ROOT\MIME\Database\Content Type\*' | Get-Item);
Write-Progress -Activity 'Collecting MIME info' -Status 'Reading Extensions' -PercentComplete 0;
$ExtensionItems = @(Resolve-Path 'Microsoft.PowerShell.Core\Registry::HKEY_CLASSES_ROOT\.*' | Get-Item);
$TotalCount = [Convert]::ToSingle($ContentTypeItems.Count) + [Convert]::ToSingle($ExtensionItems.Count);
$PercentComplete = -1;
for ($i = 0; $i -lt $ContentTypeItems.Count; $i++) {
    $p = [Convert]::ToInt32(([Convert]::ToSingle($i) * 100.0) / $TotalCount);
    if ($p -ne $PercentComplete) {
        Write-Progress -Activity 'Collecting MIME info' -Status 'Processing Content Types' -PercentComplete $p;
        $PercentComplete = $p;
    }
    $Item = $ContentTypeItems[$i];
    $Extension = $Item.GetValue('Extension', '');
    if ($Extension.Length -gt 0) {
        $MimeType = $Item.PSChildName;
        if ($MimeDictionary.ContainsKey($MimeType)) {
            if ($MimeDictionary[$MimeType] -inotcontains $Extension) { $MimeDictionary[$MimeType] = $MimeDictionary[$MimeType] + @($Extension) }
        } else {
            $MimeDictionary[$MimeType] = @($Extension);
        }
        if (-not $ExtensionDictionary.ContainsKey($Extension)) {
            $Properties = @{ Extension = $Extension; ProgId = '' };
            $m = $Regex.Match($MimeType);
            switch ($Properties['Type']) {
                'text' { $Properties['PerceivedType'] = 'text'; break; }
                'image' { $Properties['PerceivedType'] = 'image'; break; }
                'audio' { $Properties['PerceivedType'] = 'audio'; break; }
                default { $Properties['PerceivedType'] = '' }
            }
            if ($m.Success) {
                $Properties['Type'] = $m.Groups['t'].Value;
                $Properties['Subtype'] = $m.Groups['s'].Value;
                if ($m.Groups['x'].Success) {
                    $Properties['TypeSuffix'] = $m.Groups['x'].Value;
                } else {
                    $Properties['TypeSuffix'] = '';
                }
            } else {
                $p = $MimeType.Split('/', 2);
                $Properties['Type'] = $p[0];
                if ($p.Length -eq 2) {
                    $p = $p[1].Split('+', 2);
                    $Properties['Subtype'] = $p[0];
                    if ($p.Length -eq 2) {
                        $Properties['TypeSuffix'] = $p[1];
                    } else {
                        $Properties['TypeSuffix'] = '';
                    }
                } else {
                    $Properties['Subtype'] = '';
                    $Properties['TypeSuffix'] = '';
                }
            }
            $ExtensionDictionary[$Extension] = New-Object -TypeName 'System.Management.Automation.PSObject' -Property $Properties;
        }
    }
}
$OffsetCount = $ContentTypeItems.Count;
for ($i = 0; $i -lt $ExtensionItems.Count; $i++) {
    $p = [Convert]::ToInt32(([Convert]::ToSingle($i + $OffsetCount) * 100.0) / $TotalCount);
    if ($p -ne $PercentComplete) {
        Write-Progress -Activity 'Collecting MIME info' -Status 'Processing Extensions' -PercentComplete $p;
        $PercentComplete = $p;
    }
    $Item = $ExtensionItems[$i];
    $cts = $Item.GetValue('Content Type', '');
    $PerceivedType = $Item.GetValue('PerceivedType', '');
    if ($cts.Length -gt 0 -or $PerceivedType.Length -gt 0) {
        if ($cts.Length -eq 0) {
            switch ($PerceivedType) {
                'video' {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
                'audio' {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
                'image' {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
                'text' {
                    $cts = [System.Net.Mime.MediaTypeNames+Text]::Plain;
                    break;
                }
                'compressed' {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
                'document' {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
                default {
                    $cts = [System.Net.Mime.MediaTypeNames+Application]::Octet;
                    break;
                }
            }
        }

        $Properties = @{ Extension = $Item.PSChildName; PerceivedType = $PerceivedType; ProgId = $Item.GetValue('', '') };
        $m = $Regex.Match($cts);
        if ($m.Success) {
            $Properties['Type'] = $m.Groups['t'].Value;
            $Properties['Subtype'] = $m.Groups['s'].Value;
            if ($m.Groups['x'].Success) {
                $Properties['TypeSuffix'] = $m.Groups['x'].Value;
            } else {
                $Properties['TypeSuffix'] = '';
            }
        } else {
            $p = $cts.Split('/', 2);
            $Properties['Type'] = $p[0];
            if ($p.Length -eq 2) {
                $p = $p[1].Split('+', 2);
                $Properties['Subtype'] = $p[0];
                if ($p.Length -eq 2) {
                    $Properties['TypeSuffix'] = $p[1];
                } else {
                    $Properties['TypeSuffix'] = '';
                }
            } else {
                $Properties['Subtype'] = '';
                $Properties['TypeSuffix'] = '';
            }
        }
        if ($Properties['PerceivedType'].Length -eq 0) {
            switch ($Properties['Type']) {
                'text' { $Properties['PerceivedType'] = 'text'; break; }
                'image' { $Properties['PerceivedType'] = 'image'; break; }
                'audio' { $Properties['PerceivedType'] = 'audio'; break; }
            }
        }
        New-Object -TypeName 'System.Management.Automation.PSObject' -Property $Properties;
        if ($ExtensionDictionary.ContainsKey($Properties['Extension'])) {
            $m = $ExtensionDictionary[$Properties['Extension']];
            $ExtensionDictionary.Remove($Properties['Extension']);
            if ($m.Type -ne $Properties['Type'] -or $m.Subtype -ne $Properties['Subtype'] -or $m.TypeSuffix -ne $Properties['TypeSuffix']) {
                $m | Write-Output;
            }
        }
        if ($MimeDictionary.ContainsKey($cts)) {
            $MimeDictionary[$cts] = $MimeDictionary[$cts] + @($Properties['Extension']);
        } else {
            $MimeDictionary[$cts] = @($Properties['Extension']);
        }
    }
};
Write-Progress -Activity 'Collecting MIME info' -Status 'Finished' -PercentComplete 100 -Completed;
$ExtensionDictionary.Count;
#>

