// @ts-check

const { tmpdir } = require("os");
const path = require("path");
const { exec, execSync, spawn } = require("child_process");
const { requestAsync } = require("./test-utils");
const assert = require("assert");

const azlp = {
  fn: path.resolve("examples\\hello_world.linq"),
  dir: path.resolve("CloudPad.FunctionApp\\bin\\Release\\net461")
};

console.log(`compiling output into '${azlp.dir}'...`);

console.log(
  execSync(
    `\"C:\\Program Files (x86)\\LINQPad5\\LPRun.exe\" ${
      azlp.fn
    } -compile -output ${azlp.dir}`
  ).toString()
);

const func = spawn(
  require.resolve("azure-functions-core-tools/bin/func.exe"),
  ["host", "start"],
  {
    cwd: azlp.dir,
    stdio: "inherit"
  }
);

//func.stdout.on("data", data => process.stdout.write(data));
//func.stderr.on("data", data => process.stderr.write(data));

func.on("close", code => {
  console.log(`function host closed with exit code ${code}`);
  if (code !== 0) {
    process.exit(code);
  }
});

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function test() {
  for (;;) {
    await delay(1000);
    try {
      await requestAsync("http://localhost:7071/");
      break;
    } catch (err) {
      if (err.code === "ECONNREFUSED") continue;
      throw err;
    }
  }

  const res1 = await requestAsync("http://localhost:7071/api/test");
  assert.equal(res1.content, "hello world");

  const res2 = await requestAsync("http://localhost:7071/api/test-async");
  assert.equal(res2.content, "hello world asynchronous");

  console.log("\r\n✔️  test succeeded");

  func.kill();
}

test().catch(err => {
  console.error("\r\n❌  test failed");
  console.error(err);

  func.kill();

  process.exit(1);
});
