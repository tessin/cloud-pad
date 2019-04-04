import child_process = require("child_process");

import { statSync, readFileSync } from "fs";
import { homedir } from "os";
import { join } from "path";

import jwt = require("./jwt");

export function az(args: string[]) {
  return new Promise((resolve, reject) => {
    const p = child_process.spawn("az", args, {
      shell: true,
      stdio: "inherit"
    });

    p.on("error", reject);

    p.on("close", resolve);
  });
}

// ====

type AccessTokenJSON = {
  resource: string;
  accessToken: string;
};

export async function getAccessToken(
  resource: string = "https://management.core.windows.net/"
): Promise<string | undefined> {
  const accessTokensFilename = join(homedir(), ".azure", "accessTokens.json");

  if (isFile(accessTokensFilename)) {
    let accessTokens: AccessTokenJSON[] | undefined;
    try {
      accessTokens = JSON.parse(
        readTextSync(accessTokensFilename)
      ) as AccessTokenJSON[];
    } catch {
      // ...
    }

    if (accessTokens) {
      const accessToken = accessTokens.find(
        accessToken => accessToken.resource === resource
      );
      if (accessToken) {
        const { exp } = decodeClaims(accessToken.accessToken);
        if (jwt.now() < exp - 300) {
          return accessToken.accessToken;
        }
      }
    }
  }

  const { accessToken } = JSON.parse(
    child_process
      .execSync(`az account get-access-token --resource ${resource}`)
      .toString("utf8")
  ) as { accessToken: string };

  return accessToken;
}

type AzureProfile = {
  subscriptions: { id: string; name: string; isDefault: boolean }[];
};

export function getSubscription(): string {
  const azureProfileFilename = join(homedir(), ".azure", "azureProfile.json");

  if (isFile(azureProfileFilename)) {
    let azureProfile: AzureProfile | undefined;
    try {
      azureProfile = JSON.parse(
        readTextSync(azureProfileFilename)
      ) as AzureProfile;
    } catch (err) {
      console.error(err);
    }
    if (azureProfile) {
      for (const sub of azureProfile.subscriptions) {
        if (sub.isDefault) {
          return sub.id;
        }
      }
    }
  }

  throw new Error(
    "cannot get subscription, use 'az account set --s <id>' to set a default subscription"
  );
}

// ====

interface Claims {
  /** Identifies the intended recipient of the token. In id_tokens, the audience is your app's Application ID, assigned to your app in the Azure portal. Your app should validate this value, and reject the token if the value does not match. */
  aud: string;

  /** The immutable identifier for an object in the Microsoft identity system, in this case, a user account. This ID uniquely identifies the user across applications - two different applications signing in the same user will receive the same value in the oid claim. The Microsoft Graph will return this ID as the id property for a given user account. Because the oid allows multiple apps to correlate users, the profile scope is required in order to receive this claim. Note that if a single user exists in multiple tenants, the user will contain a different object ID in each tenant - they are considered different accounts, even though the user logs into each account with the same credentials. */
  oid: string;

  /** The principal about which the token asserts information, such as the user of an app. This value is immutable and cannot be reassigned or reused. The subject is a pairwise identifier - it is unique to a particular application ID. Therefore, if a single user signs into two different apps using two different client IDs, those apps will receive two different values for the subject claim. This may or may not be desired depending on your architecture and privacy requirements. */
  sub: string;

  exp: number;
  iat: number;
  nbf: number;
}

function decodeClaims<T extends Claims = Claims>(accessToken: string) {
  return jwt.decodePart<T>(accessToken.split(".")[1]);
}

// ====

function isFile(path: string) {
  try {
    return statSync(path).isFile();
  } catch {
    return false;
  }
}

function readTextSync(filename: string) {
  let buffer = readFileSync(filename);

  // byte order mark?
  if (3 <= buffer.length) {
    if (
      buffer.readUInt8(0) === 0xef &&
      buffer.readUInt8(1) === 0xbb &&
      buffer.readUInt8(2) === 0xbf
    ) {
      buffer = buffer.slice(3);
    }
  }

  return buffer.toString("utf8");
}
