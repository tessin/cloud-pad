
param(
    [parameter(Mandatory=$true, Position=0)]
    [string]$LINQPadScript
)

$LINQPadScriptSource = [System.IO.File]::ReadAllText((Resolve-Path $LINQPadScript).Path)
$QueryEndIndex = $LINQPadScriptSource.IndexOf('</Query>')
$Query = $LINQPadScriptSource.Substring(0, $QueryEndIndex + '</Query>'.Length)
$Query = [xml]$Query

$refs = @()

foreach($ref in $Query.DocumentElement.SelectNodes('/Query/Reference')) {
    $refs += $ref.InnerText
}

echo $refs
