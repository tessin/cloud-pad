
# Some useful references for the contents of this script
# https://cmatskas.com/deploying-azure-functions-with-arm-templates-and-the-kudu-rest-api/

param(
    [parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    [switch]$ResourceGroupDeployment=$true,
    [ValidateSet('dynamic', 'basic')]
    [string]$SKU='dynamic',
    [string]$PublishSettingsFile = '.\AzureFn.PublishSettings',
    [switch]$Force # allow overwrite of PublishSettingsFile
)

function Probe-Path() {
  param(
    [parameter(Mandatory=$true, Position=0)]
    [string]$Path
  )
  return Join-Path $PSScriptRoot $Path
}

#################################################################
# Resource group deployment
#################################################################

if ($ResourceGroupDeployment) { # Begin resource group deployment

if ((Test-Path -PathType Leaf $PublishSettingsFile) -and !$Force) {
  Write-Error "There's already an '$PublishSettingsFile' delete or make sure you are in the right directory? (note: the script will recreate the publishing profile if that's what you want)"
  exit 1
}

$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
    -TemplateFile (Probe-Path .\azuredeploy.json) `
    -TemplateParameterObject @{sku=$SKU;} `
    -Verbose

$storageAccountName = $deployment.Outputs['storageAccount-name'].Value
$fileShareName = $deployment.Outputs['fileShare-name'].Value
$webAppName = $deployment.Outputs['webApp-name'].Value

#################################################################
# Configure storage account and file share
#################################################################

$storageAccountKeys = Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $storageAccountName

$storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=$storageAccountName;AccountKey=$($storageAccountKeys[0].Value)"

$storageContext = New-AzureStorageContext -ConnectionString $storageAccountConnectionString

$storageShare = Get-AzureStorageShare -Name $fileShareName -Context $storageContext -ErrorAction SilentlyContinue
if (!$storageShare) {
    $storageShare = New-AzureStorageShare -Name $fileShareName -Context $storageContext
}

$webApp = Get-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webAppName

$webAppSettings = @{
    'AzureWebJobsDashboard'=$storageAccountConnectionString;
    'AzureWebJobsStorage'=$storageAccountConnectionString;
    'FUNCTIONS_EXTENSION_VERSION'='~1';
    'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'=$storageAccountConnectionString;
    'WEBSITE_CONTENTSHARE'=$fileShareName;
    'WEBSITE_NODE_DEFAULT_VERSION'='6.5.0';
}

[void](Set-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webAppName -AppSettings $webAppSettings)

#################################################################
# Download publishing profile for continuous deployment
#################################################################

# there's something off about the -OutputFile parameter
$PublishSettings = Get-AzureRmWebAppPublishingProfile `
    -ResourceGroupName $ResourceGroupName `
    -Name $webAppName `
    -OutputFile $PublishSettingsFile

$PublishSettings > $PublishSettingsFile # fix for -OutputFile parameter

} # End resource group deployment

#################################################################
# Install dependencies
#################################################################

if (!(Test-Path -PathType Leaf $PublishSettingsFile)) {
    Write-Error "'$PublishSettingsFile' not found. Redo resource group deployment, if necessary."
    exit 1
}

$dataFolder = Probe-Path data

[void](mkdir $dataFolder -ErrorAction SilentlyContinue)

#################################################################
# Install LINQPad to Azure runtime
#################################################################

if (!(Test-Path -PathType Leaf "$dataFolder\LINQPad5-AnyCPU.zip")) {
    Invoke-WebRequest -Uri 'http://www.linqpad.net/GetFile.aspx?LINQPad5.zip' -OutFile "$dataFolder\LINQPad5-AnyCPU.zip"
}

$xml = [xml](cat $PublishSettingsFile)

$publishUrl = $xml.publishData.publishProfile[0].publishUrl
$username = $xml.publishData.publishProfile[0].userName
$password = $xml.publishData.publishProfile[0].userPWD
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username,$password)))
$userAgent = "powershell/1.0"

$apiUrl = "https://$publishUrl"

Invoke-RestMethod -Method PUT `
    -Uri "$apiUrl/api/vfs/data/LINQPad5-AnyCPU/" `
    -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} `
    -UserAgent $userAgent `
    -ErrorAction SilentlyContinue

Invoke-RestMethod -Method PUT `
    -Uri "$apiUrl/api/zip/data/LINQPad5-AnyCPU" `
    -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} `
    -UserAgent $userAgent `
    -InFile "$dataFolder\LINQPad5-AnyCPU.zip"

#################################################################
# Install tooling (for deployment of LINQPad scripts)
#################################################################

if (!(Test-Path -PathType Leaf "$dataFolder\tools.zip")) {
    Invoke-WebRequest -Uri 'https://github.com/tessin/AzureLINQPadFunctions/releases/download/1.0.0/tools.zip' -OutFile "$dataFolder\tools.zip"
}

Add-Type -Assembly System.IO.Compression.FileSystem

[System.IO.Compression.ZipFile]::ExtractToDirectory("$dataFolder\tools.zip", $PSScriptRoot)

.\lp2azfn --install
