// @ts-check

const { execSync } = require("child_process");

function getEnvironment() {
  // see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/how-to-set-environment-variables-for-the-visual-studio-command-line

  const result = execSync(
    '"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\VsDevCmd" -no_logo && set'
  );

  const env = {};

  for (const item of result.toString().split("\r\n")) {
    if (item) {
      const index = item.indexOf("=");
      if (-1 < index) {
        env[item.substr(0, index)] = item.substr(index + 1);
      }
    }
  }

  // console.log(env);

  return env;
}

const env = getEnvironment();

function msbuild(solutionFile, targets) {
  return execSync(
    `msbuild ${solutionFile} /nologo ${targets
      .map(target => "/t:" + target)
      .join(" ")} /p:Configuration=Release /clp:ErrorsOnly;Summary /m`,
    { env }
  ).toString();
}

module.exports = {
  msbuild
};
