import { getWebAppPublishProfile } from "./az-rm";
import { getResourceNames } from "./id";
import { putFile } from "./put-file";

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

  const response = await putFile(
    "PUT",
    `https://${functionAppName}.scm.azurewebsites.net/api/zipdeploy`,
    "../CloudPad.FunctionApp/bin/Release/net461/host.zip",
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
