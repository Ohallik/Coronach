param([string]$SourcePath=(Join-Path $PSScriptRoot 'exec.ps1'))
$ErrorActionPreference='Stop'
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($SourcePath),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count){throw 'Source failed to parse'}
$clause=$ast.Find({param($n) $n -is [Management.Automation.Language.CatchClauseAst]},$true)
if(-not $clause){throw 'Missing execute error policy'}
$body=$clause.Body.Extent.Text
$policy=[scriptblock]::Create($body.Substring(1,$body.Length-2))
function Can-Retry([string]$content,[bool]$PackageRetry=$false){
    $retryKnownPackage=$false
    try{. $policy;return $retryKnownPackage}catch{return $false}
}
$known="Library\PackageCache\com.unity.shadergraph@123\Editor\Target.cs(1,1): error CS0246: The type or namespace name 'GUID' could not be found"
$own="Assets\Game.cs(1,1): error CS0246: The type or namespace name 'GUID' could not be found"
$cases=@(
    @{name='known CRLF';text=$known+"`r`n";expected=$true},
    @{name='known LF';text=$known+"`n";expected=$true},
    @{name='multiple known';text=$known+"`r`n"+$known;expected=$true},
    @{name='project error';text=$own;expected=$false},
    @{name='mixed compiler errors';text=$known+"`r`n"+$own;expected=$false},
    @{name='wrong compiler error';text=$known.Replace('CS0246','CS1002');expected=$false},
    @{name='unlicensed editor';text=$known+"`r`nNo valid Unity Editor license";expected=$false},
    @{name='explicit failure';text=$known+"`r`nFAILED: build";expected=$false},
    @{name='package resolution failure';text='Failed to rename package [EPERM]';expected=$false},
    @{name='no compiler errors';text='Still importing';expected=$false}
)
$failures=@()
foreach($case in $cases){if((Can-Retry $case.text) -ne $case.expected){$failures+=$case.name}}
if(Can-Retry $known $true){$failures+='must never retry twice'}
if($failures.Count){throw ('PACKAGE_RETRY_RED '+($failures -join ', '))}
Write-Output 'PACKAGE_RETRY_OK 11 cases; actual execute catch policy'
