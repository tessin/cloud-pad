import { az } from "./az";

import { getResourceNames } from "./id";

async function main() {
  const {
    resourceGroupName,
    storageAccountName,
    functionAppName
  } = getResourceNames();

  // ====

  const location = "westeurope";

  const l = ["-l", location];

  const g = ["-g", resourceGroupName];

  // ====

  await az(["group", "create", ...l, "-n", resourceGroupName]);

  await az([
    "storage",
    "account",
    "create",
    ...l,
    ...g,
    "-n",
    storageAccountName,
    "--sku",
    "Standard_LRS"
  ]);

  await az([
    "functionapp",
    "create",
    ...g,
    "-c",
    location, // consumption plan
    "-n",
    functionAppName,
    "-s",
    storageAccountName,
    "--os-type",
    "Windows",
    "--runtime",
    "dotnet"
  ]);

  await az([
    "functionapp",
    "config",
    "appsettings",
    "set",
    ...g,
    "-n",
    functionAppName,
    "--settings",
    "FUNCTIONS_EXTENSION_VERSION=~1"
  ]);
}

main().catch(console.error);
