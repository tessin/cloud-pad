// @ts-check

const { msbuild } = require("./build-utils");

msbuild("CloudPad.sln", ["CloudPad:Pack", "CloudPad_FunctionApp"]);
