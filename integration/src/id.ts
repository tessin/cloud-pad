import { statSync, readFileSync, writeFileSync } from "fs";

export function getId() {
  const fn = "id.txt";

  let isFile: boolean;
  try {
    isFile = statSync(fn).isFile();
  } catch {
    isFile = false;
  }

  if (isFile) {
    return readFileSync(fn, "utf8");
  }

  const id = Math.floor(Math.pow(2, 40) * Math.random())
    .toString(32)
    .padStart(8, "0");

  writeFileSync(fn, id);

  return id;
}

export function getResourceNames() {
  const id = getId();

  const resourceGroupName = "CloudPadTest_" + id;
  const storageAccountName = "cloudpadtest" + id;
  const functionAppName = "cloudpadtest-" + id;

  return {
    resourceGroupName,
    storageAccountName,
    functionAppName
  };
}
