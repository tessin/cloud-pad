@echo off

dotnet pack -c Release CloudPad.FunctionApp
if %errorlevel% neq 0 exit /b %errorlevel%

"C:\Program Files (x86)\LINQPad5\LPRun.exe" publish.linq CloudPad.FunctionApp\bin\Release\net461\publish

