$PrimarySigInfo = @(
    @(@{ name='stream'; type='Stream'; }, @{ name='encoding'; type='Encoding'; }, @{ name='detectEncodingFromByteOrderMarks'; type='bool'; }, @{ name='bufferSize'; type='int'; },
        @{ name='leaveOpen'; type='bool'; }, @{ name='omitCrFromNewLine'; type='bool' }),
    @(@{ name='path'; type='string'; }, @{ name='encoding'; type='Encoding'; }, @{ name='detectEncodingFromByteOrderMarks'; type='bool'; }, @{ name='bufferSize'; type='int'; },
        @{ name='omitCrFromNewLine'; type='bool' })
);

$Signatures = $PrimarySigInfo | ForEach-Object {
    $params = $_ | ForEach-Object {
        $_['key'] = $_.type + ' ' + $_.name;
        New-Object -TypeName 'System.Management.Automation.PSObject' -Property $_
    };
    New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{
        params = $params;
        count = $params.Count;
        key = ($_ | ForEach-Object { $_.type }) -join ','
    }
}

$AllArgs = $Signatures | ForEach-Object { $_.key | Write-Host;
    for ($i = 1; $i -lt $_.Params.Count; $i++) {
        New-Object -TypeName 'System.Management.Automation.PSObject' -Property @{ p = $_.params[$i]; o = $i; key = $_.params[$i].key }
    }
} | Group-Object -Property 'key' | ForEach-Object { @($_.Group | Sort-Object -Property 'o')[0] } | Sort-Object -Property 'o' | ForEach-Object { $_.p };

$AllArgs | ForEach-Object {
    $ParamToRemove = $_.key;
    "$ParamToRemove >" | Write-Host
    $Candidates = @($Signatures | Where-Object { @($_.params | Where-Object { $_.key -eq $ParamToRemove }).Count -gt 0 } | ForEach-Object {
        $params = @($_.params | Where-Object { $_.key -ne $ParamToRemove });
        $parent = $_;
        @{
            params = $params;
            count = $params.Count; 
            key = ($params | ForEach-Object { $_.type }) -join ',';
            parent = $parent;
        }
    });
    $Candidates | ForEach-Object {
        $key = $_.key;
        if (@($Signatures | Where-Object { $_.key -eq $key }).Count -eq 0) {
            $Signatures = $Signatures + @(New-Object -TypeName 'System.Management.Automation.PSObject' -Property $_);
        }
    }
}

$Signatures | Sort-Object -Property 'count' -Descending | ForEach-Object {
    if ($_.parent -ne $null) {
        @"
        public RewindableStreamReader($(($_.params | ForEach-Object { $_.key }) -join ', ')) : this($(($_.parent.params | ForEach-Object { $_.name }) -join ', ')) { }

"@
    }
}