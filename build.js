// @ts-check

const { msbuild } = require("./build-utils");

console.log(msbuild("CloudPad.sln", ["CloudPad:Pack", "CloudPad_FunctionApp"]));
