
# Multi script deployments to a single Azure function host

This is a scenario which we support by putting some care into how scripts are deployed and run within the Azure function context.

A precompiled Azure function is typically deployed like this:

- \bin
- \Function1\function.json
- \host.json

The precompiled function `Function1` is simply a folder containing a `function.json` file. This file tells the Azure function runtime what DLL to load and what code to execute.

When you deploy a LINQPad script as an Azure function we update these folders based on the original LINQPad script file name. For example, if we have a LINQPad script called `http_hello.linq` with a single function called `Hello`, we would create the following structure.

- .\scripts\http_hello\http_hello.linq 
- .\scripts\http_hello\bin 
- .\http_hello_Hello\function.json

This makes it possible to keep apart what functions where deployed as part of what script and we can delete all the function metadata for a particular script when we deploy the script again, so removing functions now work.

All the dependencies that belong to the LINQPad script itself goes into a special \bin folder which is nested under the `scripts\<script-file-name-without-extension>` directory.

To update a LINQPad script deployment we delete anything matching the pattern `.\scripts\<script-file-name-without-extension>` and `.\<script-file-name-without-extension>_*` recursively.

THe `CloudPad` runtime does not need to be redeployed each time a script changes.

When we run the script though, it's important that `.\scripts\<script-file-name-without-extension>` is the working directory, because it will be looking for script relative dependencies relative to that location and any reference that has a relative path will get rewritten to be put into this script specific bin directory. 