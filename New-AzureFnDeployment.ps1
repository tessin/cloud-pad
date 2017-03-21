
param(
    [parameter(Mandatory=$true)]
    [string]$LINQPadScript,
    [string]$Schedule, # for example, '0 0 * * * *' every hour, at minute 0
    [string]$PubProfileFileName = '.\AzureFn.PublishSettings'
)

if (!(Test-Path -PathType Leaf $PubProfileFileName)) {
  Write-Error "The '$PubProfileFileName' publishing profile cannot be found or is missing. Run '.\Init-AzureFnDeployment.ps1' first"
  exit 1
}

$psCommandFolder = [System.IO.Path]::GetDirectoryName($PSCommandPath)
$runScriptFileName = Join-Path $psCommandFolder 'run.csx'
$funConfigFileName = Join-Path $psCommandFolder 'function.json'

if (!(Test-Path -PathType Leaf $runScriptFileName)) {
  Write-Error "The '$runScriptFileName' run script template cannot be found or is missing"
  exit 1
}

if (!(Test-Path -PathType Leaf $funConfigFileName)) {
  Write-Error "The '$funConfigFileName' function configuration template cannot be found or is missing"
  exit 1
}

#################################################################
# Kudu REST API (common)
#################################################################

$azfnPubProfile = [xml](cat $PubProfileFileName)

$username = $xml.publishData.publishProfile[0].userName
$password = $xml.publishData.publishProfile[0].userPWD
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username,$password)))
$userAgent = "powershell/1.0"

#################################################################
# Build LINQPad deployment as Azure function
#################################################################

$deploymentItem = [System.IO.Path]::GetTempFileName() + '.zip'

#

$stagingFolder = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid())

$LINQPadScriptPath = (Resolve-Path $LINQPadScript).Path
$LINQPadScriptFolder = [System.IO.Path]::GetDirectoryName($LINQPadScriptPath)
$LINQPadScriptFileName = [System.IO.Path]::GetFileName($LINQPadScriptPath)
$LINQPadScriptWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($LINQPadScriptFileName)

$funFolder = Join-Path $stagingFolder $LINQPadScriptWithoutExtension

[void](mkdir $funFolder)

# run.csx
$runScript = [System.IO.File]::ReadAllText((Resolve-Path $runScriptFileName).Path)
$runScript = $runScript.Replace('<insert azure function name here>', $LINQPadScriptWithoutExtension)
$runScript = $runScript.Replace('<insert LINQPad script file name here>', $LINQPadScriptFileName)
[System.IO.File]::WriteAllText((Join-Path $funFolder 'run.csx'), $runScript)

# function.json
$funConfigFileName2 = Join-Path $LINQPadScriptFolder "$LINQPadScriptWithoutExtension.function.json" # prioritize this
if (!(Test-Path -PathType Leaf $funConfigFileName2)) {
    $funConfigFileName2 = $funConfigFileName # fallback
}
$funConfig = ConvertFrom-Json ([string](cat $funConfigFileName2))
if ($Schedule) {
    $funConfig.bindings[0].schedule = $Schedule
}
ConvertTo-Json $funConfig > (Join-Path $funFolder 'function.json')

# *.linq
cp $LINQPadScript (Join-Path $funFolder $LINQPadScriptFileName)

Add-Type -Assembly System.IO.Compression.FileSystem

[System.IO.Compression.ZipFile]::CreateFromDirectory($funFolder, $deploymentItem, [System.IO.Compression.CompressionLevel]::Optimal, $true)

rm -Recurse $funFolder

#################################################################
# Deploy LINQPad script as Azure function
#################################################################

$apiUrl = "https://$($username.TrimStart('$')).scm.azurewebsites.net/api/zip/site/wwwroot"

$res = Invoke-RestMethod -Method PUT `
    -Uri $apiUrl `
    -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} `
    -UserAgent $userAgent `
    -InFile $deploymentItem

rm $deploymentItem

Write-Host 'DONE'
