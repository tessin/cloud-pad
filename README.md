#

# Supported bindings

Technically, all the 1.x bindings are supported but we don't support intermingled input and output parameters. If you need this level of flex, go write an Azure Function instead.

https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings#supported-bindings

## HttpTrigger

While you can specify a route, you cannot get the route data by putting a parameter with the same name as a parameter to your function. (CloudPad does parameter binding differently).
