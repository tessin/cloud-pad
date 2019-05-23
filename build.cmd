@echo off

dotnet pack -c Release CloudPad.FunctionApp

"C:\Program Files (x86)\LINQPad5\LPRun.exe" publish.linq CloudPad.FunctionApp\bin\Release\net461\publish

