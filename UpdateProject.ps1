$Script:ProjectPath = 'C:\LennyTemp\pswebsrv-master\PSModule\PSWebSrv.csproj';

Function Get-ElementText {
    [OutputType([string])]
    Param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlElement]$Element,
        
        [Parameter(Mandatory = $true)]
        [ValidateScript({ [System.Xml.XmlConvert]::EncodeLocalName($_) -eq $_ })]
        [string]$Name
    )

    [System.Xml.XmlElement]$XmlElement = $Element.SelectSingleNode("msb:$Name", $Script:Nsmgr);
    if ($XmlElement -eq $null -or $XmlElement.IsEmpty) { return  '' }
    return $XmlElement.InnerText;
}

Function Get-AttributeText {
    [OutputType([string])]
    Param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlElement]$Element,
        
        [Parameter(Mandatory = $true)]
        [ValidateScript({ [System.Xml.XmlConvert]::EncodeLocalName($_) -eq $_ })]
        [string]$Name
    )

    $XmlAttribute = $Element.SelectSingleNode("@$Name");
    if ($XmlAttribute -eq $null) { return  '' }
    return $XmlAttribute.Value;
}

Function Get-ItemGroupElement {
    [OutputType([System.Xml.XmlElement])]
    Param(
        [Parameter(Mandatory = $true)]
        [ValidateScript({ [System.Xml.XmlConvert]::EncodeLocalName($_) -eq $_ })]
        [string]$Name
    )

    if ($Script:ItemGroupNodes[$Name] -ne $null) { return $Script:ItemGroupNodes[$Name] }

    $priority = $Script:ItemGroupOrder.IndexOf($Name);
    if ($priority -lt 0) {
        $priority = $Script:ItemGroupOrder.Count;
        $Script:ItemGroupOrder.Add($Name);
    }
    $InsertBefore = $null;
    for ($i = $priority + 1; $i -lt $Script:ItemGroupOrder.Count; $i++) {
        if ($Script:ItemGroupOrder[$i] -ne $null) {
            $InsertBefore = $Script:ItemGroupOrder[$i];
            break;
        }
    }
    if ($InsertBefore -eq $null) {
        $InsertAfter = $null;
        $InsertAfter = $Script:ItemGroupOrder | Select-Object { if ($Script:ItemGroupNodes[$_] -ne $null) { $Script:ItemGroupNodes[$_] } } | Select-Object -First 1;
        if ($InsertAfter -eq $null) {
            $Script:ItemGroupNodes[$Name] = $Script:CsProj.DocumentElement.AppendChild($Script:CsProj.CreateElement('ItemGroup', $Xmlns));
        } else {
            $e = $InsertAfter.SelectSingleNode('following-sibling::msb:ItemGroup[1][count(msb:*)=0]', $Nsmgr)
            if ($e -eq $null) {
                $Script:ItemGroupNodes[$Name] = $Script:CsProj.DocumentElement.InsertAfter($Script:CsProj.CreateElement('ItemGroup', $Xmlns), $InsertAfter);
            } else {
                $Script:ItemGroupNodes[$Name] = $e;
            }
        }
    } else {
        $e = $InsertBefore.SelectSingleNode('preceding-sibling::msb:ItemGroup[1][count(msb:*)=0]', $Nsmgr);
        if ($e -eq $null) {
            $Script:ItemGroupNodes[$Name] = $Script:CsProj.DocumentElement.InsertBefore($Script:CsProj.CreateElement('ItemGroup', $Xmlns), $InsertBefore);
        } else {
            $Script:ItemGroupNodes[$Name] = $e;
        }
    }

    return $Script:ItemGroupNodes[$Name];
}

Function Get-AssemblyReferenceComparisonMessages {
    [OutputType([string[]])]
    Param(
        [Parameter(Mandatory = $true)]
        [System.Reflection.AssemblyName]$Name1,

        [Parameter(Mandatory = $true)]
        [System.Reflection.AssemblyName]$Name2,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$Location1,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$Location2
    )

    if ([System.Reflection.AssemblyName]::ReferenceMatchesDefinition($Name1, $Name2)) {
    } else {
        if ($Name1.ProcessorArchitecture -ne [System.Reflection.ProcessorArchitecture]::None -and $Name2.ProcessorArchitecture -ne [System.Reflection.ProcessorArchitecture]::None -and $Name1.ProcessorArchitecture -ne $Name2.ProcessorArchitecture) {
            'Processor Architecture specifications do not match';
        }
        if (-not ([String]::IsNullOrEmpty($Location1) -or [String]::IsNullOrEmpty($Location2) -or $Location1 -eq $Location2)) {
            if (Test-Path $Location1) { $Location1 = ($Location1 | Resolve-Path).Path }
            if (Test-Path $Location2) { $Location2 = ($Location2 | Resolve-Path).Path }
            if ($Location1 -ne $Location2) { 'Hint Location does not match' }
        }
    }
}

Function Search-ItemGroupElementChanges {
    Param()

    $ProjectDir = ((Split-Path -Path $Script:ProjectPath -Parent) | Resolve-Path).Path;
    if (-not $ProjectDir.EndsWith('\')) { $ProjectDir = "$ProjectDir\" }

    @($CsProj.DocumentElement.SelectNodes('msb:ItemGroup/msb:*', $Nsmgr)) | ForEach-Object {
        $e = Get-ItemGroupElement -Name $_.LocalName;
        if (-not [Object]::ReferenceEquals($e, $_.ParentNode)) {
            $n = $Script:CsProj.ImportNode($_.CloneNode($true), $true)
            $e.AppendChild($n) | Out-Null;
            $_.ParentNode.RemoveChild($_) | Out-Null;
        }
    }
    $AllPaths = @();
    $Exclude = @($ProjectPath) + @(@($CsProj.DocumentElement.SelectNodes('msb:PropertyGroup/msb:OutputPath', $Nsmgr)) | ForEach-Object {
        if ($_.IsEmpty) { return }
        $s = $_.InnerText;
        if ($_.InnerText.Length -gt 0) {
            $p = $ProjectDir | Join-Path -ChildPath $_.InnerText;
            if ($p | Test-Path -PathType Container) {
                (Get-ChildItem -LiteralPath $p -Recurse) | ForEach-Object { $_.FullName }
            } else {
                if ($p | Test-Path) { ($p | Resolve-Path).Path }
            }
        }
    }) + @(('*.suo', '*.user', '*.sln.docstates', '.*') | ForEach-Object { Get-ChildItem -LiteralPath $ProjectDir -Filter $_ -Recurse });
    $Exclude | ForEach-Object { Write-Debug -Message "Exclude: $_" }
    (@(@($CsProj.DocumentElement.SelectNodes('msb:ItemGroup/msb:*', $Nsmgr)) | ForEach-Object {
        $Include = Get-AttributeText -Element $_ -Name 'Include';
        $Status = @();
        if ($_.LocalName -eq 'Reference') {
            $HintPath = Get-ElementText -Element $_ -Name 'HintPath';
            if ($HintPath.Length -gt 0) {
                if (-not [System.IO.Path]::IsPathRooted($HintPath)) { $HintPath = $ProjectDir | Join-Path -ChildPath $HintPath }
                if ($HintPath | Test-Path) {
                    $HintPath = ($HintPath | Resolve-Path).Path;
                    $AllPaths = $AllPaths + @($HintPath);
                } else {
                    $Status = @('Hint path not found.');
                }
            }
            try {
                $n = [System.Reflection.AssemblyName]::new($Include);
                $m = $Script:KnownAssemblies | Where-Object { [System.Reflection.AssemblyName]::ReferenceMatchesDefinition($n, $_.Name) } | Select-Object -First 1;
                if ($m -eq $null) {
                    $a = $null;
                    try { $a = [System.Reflection.Assembly]::ReflectionOnlyLoad($n) }
                    catch {
                        try { $a = [System.Reflection.Assembly]::LoadWithPartialName($n.ToString()) }
                        catch {
                            if ($HintPath.Length -gt 0 -and $HintPath | Test-Path -PathType Leaf) { try { $a = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($HintPath) } catch { } }
                        }
                    }
                    if ($a -ne $null) {
                        $m = New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $a.GetName($true); Location = $a.Location };
                        $Script:KnownAssemblies = $Script:KnownAssemblies + @($m);
                    }
                }
                if ($m -eq $null) {
                    if ($HintPath.Length -gt 0 -and $HintPath | Test-Path -PathType Leaf) { $Status = $Status + @('Unable to load assembly.') } else { $Status = $Status + @('Unable to load assembly or assembly not found.') }
                } else {
                    if (@(Get-AssemblyReferenceComparisonMessages -Name1 $n -Name2 $m.Name).Count -gt 0) {
                        if ($HintPath.Length -gt 0 -and $HintPath | Test-Path -PathType Leaf) {
                            $a = $null;
                            try { $a = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($HintPath) } catch { }
                            if ($a -eq $null) {
                                $Status = $Status + @('Unable to load assembly.');
                            } else {
                                $m = New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $a.GetName($true); Location = $a.Location };
                                $Script:KnownAssemblies = $Script:KnownAssemblies + @($m);
                            }
                        } else {
                            $Status = $Status + @(Get-AssemblyReferenceComparisonMessages -Name1 $n -Name2 $m.Name -Location1 $HintPath -Location2 $m.Location);
                        }
                    }
                }
            } catch {
                $Status = 'Invalid Name';
            }
        } else {
            $FullPath = $ProjectDir | Join-Path -ChildPath $Include;
            if ($FullPath | Test-Path -PathType Leaf) {
                $AllPaths = $AllPaths + @(($FullPath | Resolve-Path).Path);
                if ($_.LocalName -eq 'Folder' -or $_.LocalName -eq 'AppDesigner') {
                    $Status = $Status + @('Include is not a folder.');
                } else {
                    $DependentUpon = Get-ElementText -Element $_ -Name 'DependentUpon';
                    if (-not $DependentUpon.Length -eq 0) {
                        if ($DependentUpon | Test-Path -PathType Container) {
                            $Status = $Status + @('DependentUpon is not a file.');
                        } else {
                            if (-not $DependentUpon | Test-Path -PathType Leaf) { $Status = $Status + @('DependentUpon file not found.') }
                        }
                    }
                    $LastGenOutput = Get-ElementText -Element $_ -Name 'LastGenOutput';
                    if (-not $LastGenOutput.Length -eq 0) {
                        if ($LastGenOutput | Test-Path -PathType Container) {
                            $Status = $Status + @('LastGenOutput is not a file.');
                        } else {
                            if (-not $LastGenOutput | Test-Path -PathType Leaf) { $Status = $Status + @('LastGenOutput file not found.') }
                        }
                    }
                }
            } else {
                if ($Fullpath | Test-Path -PathType Container) {
                    $AllPaths = $AllPaths + @(($FullPath | Resolve-Path).Path);
                    if ($_.LocalName -ne 'Folder' -and $_.LocalName -ne 'AppDesigner') {
                        $Status = $Status + @('Include is not a file.');
                    }
                } else {
                    if ($_.LocalName -eq 'Folder' -or $_.LocalName -eq 'AppDesigner') {
                        $Status = $Status + @('Include folder not found.');
                    } else {
                        $Status = $Status + @('Include file not found.');
                    }
                }
            }
        }
        if ($Status.Count -eq 0) { $Status = @('Ok') }
        New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{
            Type = $_.LocalName;
            Include = $Include;
            Properties = (($_.SelectNodes('msb:*', $Script:Nsmgr) | ForEach-Object { if ($_.IsEmpty) { "(empty $($p[$_.Localname]))" } else { "$($_.Localname): $($_.InnerText)" } }) | Out-String).Trim();
            Status = ($Status | Out-String).Trim();
        }
    }) + @(Get-ChildItem -LiteralPath $ProjectDir -Recurse | Where-Object { $Exclude -inotcontains $_.FullName -and $AllPaths -inotcontains $_.FullName } | ForEach-Object {
        if ($_.PSIsContainer) {
            if (@($_.FullName | Get-ChildItem).Count -eq 0) {
                $p = $_.FullName;
                if (-not $p.EndsWith('\')) { $p = "$p\" }
                New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{
                    Type = 'Unreferenced';
                    Include = $p.Substring($ProjectDir.Length);
                    Properties = ''
                    Status = 'Empty folder not referenced';
                };
            }
        } else {
            New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{
                Type = 'Unreferenced';
                Include = $_.FullName.Substring($ProjectDir.Length);
                Properties = '';
                Status = 'File not referenced';
            }
        }
    })) | Sort-Object -Property 'Type', 'Include';
}

$DebugPreference = [System.Management.Automation.ActionPreference]::Continue;

$Script:CsProj = [System.Xml.XmlDocument]::new();
$Script:CsProj.Load($Script:ProjectPath);
if ($Script:CsProj.DocumentElement -eq $null) { return }
$Script:Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";
$Script:Nsmgr = [System.Xml.XmlNamespaceManager]::new($Script:CsProj.NameTable);
$Script:Nsmgr.AddNamespace('msb', $Script:Xmlns);
[System.Collections.ObjectModel.Collection[System.String]]$Script:ItemGroupOrder = @('Reference', 'Page', 'Content', 'Compile', 'EmbeddedResource', 'None', 'Folder', 'ProjectReference', 'AppDesigner', 'Service');
$Script:KnownAssemblies = @([System.AppDomain]::CurrentDomain.GetAssemblies() | ForEach-Object { New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $_.GetName($true); Location = $_.Location } });
$Script:ItemGroupNodes = @{};
$Script:ItemGroupOrder | ForEach-Object {
    $Script:ItemGroupNodes[$_] = @($Script:CsProj.DocumentElement.SelectNodes("msb:ItemGroup[not(count(msb:$_)=0)]", $Script:Nsmgr)) | Where-Object {
        $n = $_;
        @($Script:ItemGroupNodes.Values | Where-Object { [Object]::ReferenceEquals($_, $n) }).Count -eq 0;
    } | Select-Object -First 1;
}

$Script:KnownAssemblies = $Script:KnownAssemblies + @([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory() | Get-ChildItem -Filter '*.dll' | ForEach-Object {
    $n = $null;
    try { $n = [System.Reflection.AssemblyName]::GetAssemblyName($_.FullName) } catch { }
    if ($n -ne $null -and @($Script:KnownAssemblies | Where-Object { [System.Reflection.AssemblyName]::ReferenceMatchesDefinition($_.Name, $n) }).Count -eq 0) {
        New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $n; Location = $_.FullName }
    }
});

[System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$TopLevelChoices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new('All', 'Show All Items'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Files', 'Manage Files and Folders'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Add Assembly', 'Add Assembly References'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Reload', 'Reload Project File'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Save', 'Save Project File'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Exit', 'Quit Tool')
);

[System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$AssemblyRefNameChoices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new('Full', 'Full Assembly Name'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Full Arch', 'Full Assembly Name with Processor Architecture'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Partial', 'Partial Name Only')
);
[System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$AssemblyRefVerChoices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new('True', 'Specific Version'),
    [System.Management.Automation.Host.ChoiceDescription]::new('False', 'Non-Specific Version'),
    [System.Management.Automation.Host.ChoiceDescription]::new('None', 'Not Specified')
);
[System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$AssemblyRefHintChoices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new('Yes', 'Include Hint Path'),
    [System.Management.Automation.Host.ChoiceDescription]::new('No', 'Do not include Hint Path')
);
[System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$AssemblyRefSelectChoices = @(
    [System.Management.Automation.Host.ChoiceDescription]::new('Modify', 'Modify Assembly Reference'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Delete', 'Remove Assembly Reference'),
    [System.Management.Automation.Host.ChoiceDescription]::new('Cancel', 'Do nothing')
);
$ProjectItems = @(Search-ItemGroupElementChanges);

$Index = $Host.UI.PromptForChoice('Command', 'Select command', $TopLevelChoices, 0);
while ($Index -ne $null -and $Index -ge 0 -and $Index -lt $TopLevelChoices.Count - 1) {
    switch ($TopLevelChoices[$Index].Label) {
        'All' { # Show All Items
            $Selection = $ProjectItems | Out-GridView -Title 'Project Items' -OutputMode Single;
            while ($Selection -ne $null) {
                switch ($Selection.Type) {
                    'Reference' {
                        $ReferenceElement = $Script:CsProj.DocumentElement.SelectSingleNode("msb:ItemGroup/msb:Reference[@Include=`"$($Selection.Include)`"]", $Script:Nsmgr);
                        $Index = $Host.UI.PromptForChoice('Assembly Reference', @"
Action for $($Selection.Include)
$($Selection.Properties)
"@, $AssemblyRefSelectChoices, 0);
                        if ($Index -eq 0) {
                            $Index = $Host.UI.PromptForChoice('Name Reference', "How is $($Selection.Include) to be referenced", $AssemblyRefNameChoices, 0);
                            $AssemblyRefNameOpt = 'Partial';
                            if ($Index -ge 0 -and $Index -lt $AssemblyRefNameChoices.Count - 1) { $AssemblyRefNameOpt = $AssemblyRefNameChoices[$Index] }
                            $XmlAttribute = $ReferenceElement.SelectSingleNode('@Include');
                            if ($AssemblyRefNameOpt -eq 'Partial') {
                                $XmlAttribute.Value = $_.Name.Name;
                            } else {
                                if ($AssemblyRefNameOpt -eq 'FullName' -or $_.Name.ProcessorArchitecture -eq [System.Reflection.ProcessorArchitecture]::None) {
                                    $XmlAttribute.Value = $_.Name.ToString();
                                } else {
                                    $XmlAttribute.Value = "$($_.Name.ToString()), processorArchitecture=$($_.Name.ProcessorArchitecture.ToString('F'))";
                                }
                            }

                            $Index = $Host.UI.PromptForChoice('Version Specificity', 'Indicate version specificity', $AssemblyRefVerChoices, 0);
                            $AssemblyRefVerOpt = 'None';
                            if ($Index -ge 0 -and $Index -lt $AssemblyRefVerChoices.Count - 1) { $AssemblyRefVerOpt = $AssemblyRefVerChoices[$Index] }
                            $Index = $Host.UI.PromptForChoice('Hint Path', 'Indicate Hint Path options', $AssemblyRefHintChoices, 0);
                            $AssemblyRefHintOpt = 'No';
                            if ($Index -ge 0 -and $Index -lt $AssemblyRefHintChoices.Count - 1) { $AssemblyRefHintOpt = $AssemblyRefHintChoices[$Index] }
                        } else {
                            if ($Index -eq 1) {
                                $e = $Script:CsProj.DocumentElement.SelectSingleNode("msb:ItemGroup/msb:Reference[@Include=`"$($Selection.Include)`"]", $Script:Nsmgr);
                                $e.ParentNode.RemoveChild($e) | Out-Null;
                            }
                        }
                        break;
                    }
                    'Compile' {
                        break;
                    }
                    'Content' {
                        break;
                    }
                    'Resource' {
                        break;
                    }
                    'EmbeddedResource' {
                        break;
                    }
                    'Folder' {
                        break;
                    }
                    'None' {
                        break;
                    }
                    'Unreferenced' {
                        break;
                    }
                }
                $Selection = $ProjectItems | Out-GridView -Title 'Project Items' -OutputMode Single;
            }
            break;
        }
        'Files' { # Manage Files and Folders
            $Selections = @($ProjectItems | Where-Object { $_.Type -ne 'Reference' } | Out-GridView -Title 'Project Files' -OutputMode Multiple);
            while ($Selections.Count -gt 0) {
                $Selections = @($ProjectItems | Out-GridView -Title 'Project Files' -OutputMode Multiple);
            }
            break;
        }
        'Add Assembly' { # Add Assembly References
            $NotAdded = @($Script:KnownAssemblies | Where-Object { $a = $_; @($ProjectItems | Where-Object {
                if ($_.Type -ne 'Reference') { return $false }
                try {
                    return [System.Reflection.AssemblyName]::ReferenceMatchesDefinition([System.Reflection.AssemblyName]::new($_.Include), $a.Name);
                } catch { return $true }
            }).Count -eq 0 });
            
            [System.Collections.ObjectModel.Collection[[System.Management.Automation.Host.ChoiceDescription]]]$AddAssemblyChoices = @(
                [System.Management.Automation.Host.ChoiceDescription]::new('Browse', 'Browse Framework Assemblies'),
                [System.Management.Automation.Host.ChoiceDescription]::new('Load', 'Load assembly by name'),
                [System.Management.Automation.Host.ChoiceDescription]::new('Path', 'Enter Path'),
                [System.Management.Automation.Host.ChoiceDescription]::new('Return', 'Back to main menu')
            );
            if ($NotAdded.Count -eq 0) { $AddAssemblyChoices.RemoveAt(0) }
            $Index = $Host.UI.PromptForChoice('Add Assembly', 'Select command', $AddAssemblyChoices, 0);
            while ($Index -ne $null -and $Index -ge 0 -and $Index -lt $AddAssemblyChoices.Count - 1) {
                $AssembliesToAdd = @();
                switch ($AddAssemblyChoices[$Index].Label) {
                    'Browse' {
                        $AssembliesToAdd = @($NotAdded | Out-GridView -Title 'Add Assemblies' -OutputMode Multiple);
                        break;
                    }
                    'Load' {
                        $n = $null;
                        $s = Read-Host -Prompt 'Enter assembly name';
                        if ($s.Trim().Length -gt 0) {
                            try { $n = [System.Reflection.AssemblyName]::new($s) }
                            catch {
                                $Host.UI.WriteErrorLine('Invalid assembly name');
                            }
                        }
                        if ($n -ne $null) {
                            $existing = @($Script:KnownAssemblies | Where-Object { [System.Reflection.AssemblyName]::ReferenceMatchesDefinition($n, $_.Name) });
                            if ($existing.Count -eq 0) {
                                $a = $null;
                                try { $a = [System.Reflection.Assembly]::ReflectionOnlyLoad($n) }
                                catch {
                                    try { $a = [System.Reflection.Assembly]::LoadWithPartialName($n.ToString()) }
                                    catch {
                                        $Host.UI.WriteErrorLine(($_ | Out-String).Trim());
                                    }
                                }
                                if ($a -ne $null) {
                                    $m = New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $a.GetName($true); Location = $a.Location };
                                    $Script:KnownAssemblies = $Script:KnownAssemblies + @($m);
                                    $AssembliesToAdd = @($m);
                                }
                            } else {
                                $AssembliesToAdd = @($existing[0]);
                            }
                        }
                        
                        break;
                    }
                    'Path' {
                        $r = $true;
                        while ($r) {
                            $OpenFileDialog = [Microsoft.Win32.OpenFileDialog]::new();
                            $OpenFileDialog.AddExtension = $true;
                            $OpenFileDialog.CheckFileExists = $true;
                            $OpenFileDialog.DefaultExt = '.dll';
                            $OpenFileDialog.Filter = 'DLL Files (*.dll)|*.dll|All Files (*.*)|*.*';
                            $OpenFileDialog.FilterIndex = 0;
                            $OpenFileDialog.ReadOnlyChecked = $true;
                            $OpenFileDialog.RestoreDirectory = $true;
                            $OpenFileDialog.Title = 'Browse for DLL File';
                            $r = $OpenFileDialog.ShowDialog();
                            if ($r -eq $null -or -not $r) { break }
                            try {
                                $n = $null;
                                try { $n = [System.Reflection.AssemblyName]::GetAssemblyName($OpenFileDialog.FileName) }
                                catch { $Host.UI.WriteErrorLine(($_ | Out-String).Trim()); }
                                if ($n -ne $null) {
                                    if (@($ProjectItems | Where-Object {
                                            if ($_.Type -ne 'Reference') { return $false }
                                            try {
                                                return [System.Reflection.AssemblyName]::ReferenceMatchesDefinition([System.Reflection.AssemblyName]::new($_.Include), $n);
                                            } catch { return $false }
                                        }).Count -eq 0) {
                                        $AssembliesToAdd = @(New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ Name = $n; Location = $OpenFileDialog.FileName });
                                        if (@($Script:KnownAssemblies | Where-Object { [System.Reflection.AssemblyName]::ReferenceMatchesDefinition($n, $_.Name) }).Count -eq 0) {
                                            $Script:KnownAssemblies = $Script:KnownAssemblies + @($AssembliesToAdd[0]);
                                        }
                                        $r = $false;
                                    } else {
                                        $Host.UI.WriteErrorLine('That assembly was already added.');
                                    }
                                }
                            } catch {
                                $Host.UI.WriteErrorLine(($_ | Format-List).Trim());
                            }
                        }
                        break;
                    }
                }
                if ($AssembliesToAdd.Count -gt 0) {
                    $NotAdded = @($NotAdded | Where-Object { $x = $_; @($AssembliesToAdd | Where-Object { [System.Reflection.AssemblyName]::ReferenceMatchesDefinition($x.Name, $_.Name) }).Count -gt 0 });
                    $Index = $Host.UI.PromptForChoice('Name Reference', 'How is the assembly name to be referenced', $AssemblyRefNameChoices, 0);
                    $AssemblyRefNameOpt = 'Partial';
                    if ($Index -ge 0 -and $Index -lt $AssemblyRefNameChoices.Count - 1) { $AssemblyRefNameOpt = $AssemblyRefNameChoices[$Index] }
                    $Index = $Host.UI.PromptForChoice('Version Specificity', 'Indicate version specificity', $AssemblyRefVerChoices, 0);
                    $AssemblyRefVerOpt = 'None';
                    if ($Index -ge 0 -and $Index -lt $AssemblyRefVerChoices.Count - 1) { $AssemblyRefVerOpt = $AssemblyRefVerChoices[$Index] }
                    $Index = $Host.UI.PromptForChoice('Hint Path', 'Indicate Hint Path options', $AssemblyRefHintChoices, 0);
                    $AssemblyRefHintOpt = 'No';
                    if ($Index -ge 0 -and $Index -lt $AssemblyRefHintChoices.Count - 1) { $AssemblyRefHintOpt = $AssemblyRefHintChoices[$Index] }
                    $ItemGroupElement = Get-ItemGroupElement -Name 'Reference';
                    $ProjectItems = ($ProjectItems + @($AssembliesToAdd | ForEach-Object {
                        [System.Xml.XmlElement]$ReferenceElement = $ItemGroupElement.AppendChild($Script:CsProj.CreateElement('Reference', $Script:XmlNs));
                        $Properties = @{
                            Type = 'Reference';
                            Status = 'OK';
                        }
                        $XmlAttribute = $ReferenceElement.Attributes.Append($Script:CsProj.CreateAttribute('Include'));
                        if ($AssemblyRefNameOpt -eq 'Partial') {
                            $XmlAttribute.Value = $_.Name.Name;
                        } else {
                            if ($AssemblyRefNameOpt -eq 'FullName' -or $_.Name.ProcessorArchitecture -eq [System.Reflection.ProcessorArchitecture]::None) {
                                $XmlAttribute.Value = $_.Name.ToString();
                            } else {
                                $XmlAttribute.Value = "$($_.Name.ToString()), processorArchitecture=$($_.Name.ProcessorArchitecture.ToString('F'))";
                            }
                        }
                        $Properties['Include'] = $XmlAttribute.Value;
                        $pv = @();
                        if ($AssemblyRefVerOpt -ne 'None') {
                            $pv = @("SpecificVersion: $AssemblyRefVerOpt");
                            $ReferenceElement.AppendChild($Script:CsProj.CreateElement('SpecificVersion')).InnerText = $AssemblyRefVerOpt;
                        }
                        if ($AssemblyRefVerOpt -eq 'Yes') {
                            $pv = @("HintPath: $($_.Location)");
                            $ReferenceElement.AppendChild($Script:CsProj.CreateElement('HintPath')).InnerText = $_.Location;
                        }
                        $Properties['Properties'] = ($pv | Out-String).Trim();
                        New-Object -TypeName 'System.Management.Automation.PSObject' -Property $Properties;
                    })) | Sort-Object -Property 'Type', 'Include';
                }
                if ($NotAdded.Count -eq 0 -and $AddAssemblyChoices.Count -gt 3) { $AddAssemblyChoices.RemoveAt(0) }
                $Index = $Host.UI.PromptForChoice('Add Assembly', 'Select command', $AddAssemblyChoices, 0);
            }
            break;
        }
        'Reload' { # Reload Project File
            $ProjectItems = @(Search-ItemGroupElementChanges);
            break;
        }
        'Save' { # Save Project File
            break;
        }
    }
    $Index = $Host.UI.PromptForChoice('Command', 'Select command', $TopLevelChoices, 0);
}
#}

<#[System.AppDomain]::CurrentDomain.GetAssemblies() | Sort-Object -Property 'FullName' | ForEach-Object {
    @"
    <Reference Include="$($_.FullName), processorArchitecture=MSIL">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>$($_.Location)</HintPath>
    </Reference>
"@
}#>
