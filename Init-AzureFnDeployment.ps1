
# Some useful references for the contents of this script
# https://cmatskas.com/deploying-azure-functions-with-arm-templates-and-the-kudu-rest-api/

param(
  [parameter(Mandatory=$true)]
  [string]$ResourceGroupName
)

$azfnPubProfileFileName = '.\AzureFn.PublishSettings'

if (Test-Path -PathType Leaf $azfnPubProfileFileName) {
  Write-Error "There's already an '$azfnPubProfileFileName' delete or make sure you are in the right directory? (note: the script will recreate the publishing profile if that's what you want)"
  exit 1
}

$deployment = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
    -TemplateFile .\azuredeploy.json `
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

$storageShare = Get-AzureStorageShare -Name $fileShareName -Context $storageContext
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

Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ResourceGroupName `
    -Name $webAppName `
    -OutputFile $azfnPubProfileFileName > $azfnPubProfileFileName # -OutputFile does nothing so we have to pipe to disk as well

#################################################################
# Deploy LINQPad
#################################################################

$azfnPubProfile = [xml](cat $azfnPubProfileFileName)

$username = $xml.publishData.publishProfile[0].userName
$password = $xml.publishData.publishProfile[0].userPWD
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username,$password)))
$userAgent = "powershell/1.0"

$apiUrl = "https://$webAppName.scm.azurewebsites.net/api/zip/data"

Invoke-RestMethod -Method PUT `
    -Uri $apiUrl `
    -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} `
    -UserAgent $userAgent `
    -InFile LINQPad5-AnyCPU.zip
