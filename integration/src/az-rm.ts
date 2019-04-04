import { getSubscription, getAccessToken } from "./az";

import { jsonRpc, JsonRpcError } from "./json-rpc";

export async function getWebAppPublishProfile(
  resourceGroup: string,
  name: string
) {
  const subscription = getSubscription();

  const accessToken = await getAccessToken();

  try {
    await jsonRpc(
      "POST",
      `https://management.azure.com/subscriptions/${subscription}/resourceGroups/${resourceGroup}/providers/Microsoft.Web/sites/${name}/publishxml?api-version=2018-02-01`,
      undefined,
      { authorization: `Bearer ${accessToken}` }
    );
  } catch (err) {
    if (err instanceof JsonRpcError) {
      if (err.statusCode === 200) {
        const userName = /userName="([^"]+)"/.exec(err.text)!;
        const userPWD = /userPWD="([^"]+)"/.exec(err.text)!;
        return {
          user: userName[1],
          pass: userPWD[1]
        };
      }
    }
  }

  throw new Error("cannot get publish profile");
}
