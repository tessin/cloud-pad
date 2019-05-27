#

# Supported bindings

Technically, all the 1.x bindings are supported but we don't support intermingled input and output parameters. If you need this level of flex, go write an Azure Function instead.

https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings#supported-bindings

## HttpTrigger

While you can specify a route, you cannot get the route data by putting a parameter with the same name as a parameter to your function. (CloudPad does parameter binding differently).

## QueueTrigger

Note the exponential backoff policy. If a queue has been dormant for some time, it may take up to a minute for the queue to be processed. Restarting the script may be the fastest way to get the processing moving during development but this can also be configured in the host.json file.