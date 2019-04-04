import { getWebAppPublishProfile } from "./az-rm";
import { getResourceNames } from "./id";
import { putFile } from "./put-file";
import { jsonRpc } from "./json-rpc";

async function main() {
  // https://www.linqpad.net/GetFile.aspx?LINQPad5.zip

  const { resourceGroupName, functionAppName } = getResourceNames();

  const credentials = await getWebAppPublishProfile(
    resourceGroupName,
    functionAppName
  );

  const authorization = Buffer.from(
    credentials.user + ":" + credentials.pass
  ).toString("base64");

  // ====

  const t = new Date().toISOString().split("T")[0];

  // D:\home\...

  const vfs = `site/tools/LINQPad.${t}`; // site\tools\LINQPad.*

  try {
    await jsonRpc(
      "PUT",
      `https://${functionAppName}.scm.azurewebsites.net/api/vfs/${vfs}/`,
      undefined,
      {
        authorization: `Basic ${authorization}`
      }
    );
  } catch {
    // nom nom nom...
  }

  const response = await putFile(
    "PUT",
    `https://${functionAppName}.scm.azurewebsites.net/api/zip/${vfs}`,
    "LINQPad5.zip",
    {
      authorization: `Basic ${authorization}`
    }
  );

  if (response.statusCode === 200) {
    console.log("ok");
  } else {
    console.error(response);
  }
}

main().catch(console.error);
