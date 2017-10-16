$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop;
$WarningPreference = [System.Management.Automation.ActionPreference]::Continue;
$ProgressPreference = [System.Management.Automation.ActionPreference]::Continue;
$DebugPreference = [System.Management.Automation.ActionPreference]::Continue;
$VerbosePreference = [System.Management.Automation.ActionPreference]::Continue;
$InformationPreference = [System.Management.Automation.ActionPreference]::Continue;


Function Invoke-TestScriptBlock {
    Param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Description,

        [Parameter(Mandatory = $true, Position = 1)]
        [ScriptBlock]$ScriptBlock
    )
    $Script:TestCounts++;
    $Error.Clear();
    Write-Verbose -Message "Start Test: $Description";
    try { &$ScriptBlock; }
    catch { Write-Warning -Message "Caught Error: $(($_ | Out-String).Trim())"; }
    if ($Error.Count -gt 0) {
        $Script:ErrorRecords = $Script:ErrorRecords + @(@($Error) | ForEach-Object {
            $r = $_;
            $Exception = $_.Exception;
            while ($Exception.InnerException -ne $null -and $Exception -is [System.Management.Automation.RuntimeException]) {
                $Exception = $Exception.InnerException
                if ($Exception -is [System.Management.Automation.IContainsErrorRecord] -and $Exception.ErrorRecord -ne $null) {
                    $r = $Exception.ErrorRecord;
                }
            }
            $StackTrace = '';
            try { $StackTrace = $Exception.StackTrace } catch { }
            New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{
                Test = $Description;
                Message = $r.ToString();
                Category = $r.CategoryInfo.Category;
                TargetName = $r.CategoryInfo.TargetName;
                Activity = $r.CategoryInfo.Activity;
                Position = $r.InvocationInfo.PositionMessage;
                StackTrace = $StackTrace;
                ExceptionType = $Exception.GetType().FullName;
            };
        });
        $ErrorText = (&{for ($i = 0; $i -lt $Error.Count; $i++) {
            ((("$($i + 1): $($Error[$i])" -split '\r\n?|\n') | ForEach-Object { "    " + $_ }) | Out-String).Trim();
            ((($Error[$i] | Out-String) -split '\r\n?|\n') | ForEach-Object { "  " + $_ });
        }}) | Out-String;
        ($ErrorText -split '\r\n?|\n') | ForEach-Object { $Host.UI.WriteErrorLine("  $_") }
        Write-Warning -Message ' ... Test Failed.';
    } else {
        Write-Verbose -Message ' ... Test Succeeded!';
        $Script:SuccessCounts++;
    }
}

Import-Module -Name ($PSScriptRoot | Join-Path -ChildPath '.\Deploy\Debug\Erwine.Leonard.T.PSWebSrv.psd1') -ErrorAction Stop;
$Script:TestCounts = 0;
$Script:SuccessCounts = 0;
$Script:ErrorRecords = @();

Invoke-TestScriptBlock '[Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetContentTypeFromExtension(''.txt'')' {
    $ContentType = [Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetContentTypeFromExtension('.txt');
    if ($Error.Count -gt 0) { return }
    if ($ContentType -eq $null) { throw 'Null value not expected' }
    if ($ContentType.MediaType -eq $null) { throw 'Null MediaType not expected' }
    if ($ContentType.MediaType -cne [System.Net.Mime.MediaTypeNames+Text]::Plain) {
        throw "Expected: $([System.Net.Mime.MediaTypeNames+Text]::Plain); Actual: $($ContentType.MediaType)"
    }
}

Invoke-TestScriptBlock '[Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetContentTypeFromExtension(''.xyzpdqabc'')' {
    $ContentType = [Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetContentTypeFromExtension('.xyzpdqabc');
    if ($ContentType -eq $null) { throw 'Null value not expected' }
    if ($ContentType.MediaType -eq $null) { throw 'Null MediaType not expected' }
    if ($ContentType.MediaType -cne [System.Net.Mime.MediaTypeNames+Application]::Octet) {
        throw "Expected: $([System.Net.Mime.MediaTypeNames+Application]::Octet); Actual: $($ContentType.MediaType)"
    }
}

Invoke-TestScriptBlock "[Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetExtensionFromContentType('$([System.Net.Mime.MediaTypeNames+Text]::Plain)')" {
    $Extension = [Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetExtensionFromContentType([System.Net.Mime.MediaTypeNames+Text]::Plain);
    if ($Extension -eq $null) { throw 'Null value not expected' }
    if ($Extension -cne '.txt') {
        throw "Expected: '.txt'; Actual: '$($Extension)'"
    }
}

Invoke-TestScriptBlock '[Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetExtensionFromContentType(''unknown/other'')' {
    $Extension = [Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetExtensionFromContentType('unknown/other');
    if ($Extension -eq $null) { throw 'Null value not expected' }
    if ($Extension.Length -gt 0) {
        throw "Expected empty string; Actual: '$($Extension)'"
    }
}

Invoke-TestScriptBlock '[Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetNextLineStart($null)' {
    [Erwine.Leonard.T.PSWebSrv.MimeEntities.Utility]::GetNextLineStart($null)
}

Invoke-TestScriptBlock 'Example' {
    throw "This is the duh!"
}

if ($Script:TestCounts -eq $Script:SuccessCounts) {
    Write-Verbose -Message 'All tests passed!';
} else {
    if ($Script:SuccessCounts -eq 0) {
        Write-Warning -Message 'All tests failed.';
    } else {
        $FailCounts = $Script:TestCounts - $Script:SuccessCounts;
        if ($FailCounts -eq 1) {
            if ($Script:SuccessCounts -eq 1) {
                Write-Warning -Message "1 test passed and 1 test failed.";
            } else {
                Write-Warning -Message "$Script:SuccessCounts tests passed and 1 test failed.";
            }
        } else {
            if ($Script:SuccessCounts -eq 1) {
                Write-Warning -Message "1 test passed and $FailCounts tests failed.";
            } else {
                Write-Warning -Message "$Script:SuccessCounts tests passed and $FailCounts tests failed.";
            }
        }
    }
}

if ($Script:ErrorRecords.Count -gt 0) { $Script:ErrorRecords | Out-GridView }