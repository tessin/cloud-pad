# Run LINQPad scripts as Azure functions

This is a set of PowerShell scripts and conventions to deploy and run LINQPad scripts as Azure functions.

## Requirements

You have to install the Azure Resource Manager PowerShell cmdlets.

# Getting started

## Initial deployment

If this is the first time you use this guide you need to:

1. Create a new resource group (login to the Azure Portal create a new Resource Group) if you don’t want to use an existing...
2. Run the PowerShell script `.\Init-AzureFnDeployment.ps1`

**Note** that you have to be logged in to the Azure Resource Manager for this to work, if you are not it won’t work so run run these two commands first:

~~~
Login-AzureRmAccount; `
Set-AzureRmContext -SubscriptionId <Subscription ID goes here>
~~~

## Setting up your first script

An Azure function requires a `function.json` file. If you script is called `LINQPad.linq` the name of this file should be `LINQPad.function.json` and it should reside alongside your `LINQPad.linq` script in the same directory.

Here's what this file should look like:

~~~
{
  "bindings": [
    {
      "name": "timerTrigger",
      "type": "timerTrigger",
      "direction": "in",
      "schedule": "0 0 * * * *"
    }
  ],
  "disabled": false
}
~~~

> **Note:** The `timerTrigger` is currently the only supported trigger.

### Attachments

If you LINQPad script depends on additional files that are not somehow referenced by the LINQPad script itself (like libraries and NuGet packages) you can use an attachments file.

Similar to how the `function.json` file is located, the attachments file should be `LINQPad.files.txt`. Each line in this file is a file which will be included and deployed alongside with the LINQPad script.

You can use either absolute or relative paths in this file. Relative paths will be relative ot the directory containing the LINQPad script.

## Continuous deployment

The previous step has prepared your working directory with a file `.\AzureFn.PublishSettings` which will be used to supply the credentials when needed, keep it safe.

To publish a LINQPad script as a Azure function, do this:

> `.\lp2azfn .\HelloWorld.linq`

`lp2azfn` is a binary that was _installed_ as part of running the `.\Init-AzureFnDeployment.ps1` it also added a shortcut in the `shell:sendto` folder. Which means you can right-click a LINQPad script and now send it to Azure.

The `lp2azfn` is available pre-built from the releases tab, source code is included in this repository.

# Good to know

By default we provision using the dynamic SKU which has a maximum timeout of 5 minutes.

If you need to run LINQPad scripts for a longer than 5 minutes you need to switch SKU to an always on SKU, you can do this from within the Azure Portal or by modifying the ARM template (`azuredeploy.json`).

## References

- https://github.com/Azure/azure-webjobs-sdk-script/issues/18#issuecomment-245636277
- https://github.com/Azure/azure-webjobs-sdk-script/issues/18#issuecomment-246416239
