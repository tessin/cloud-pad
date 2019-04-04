export function decodePart<T>(part: string): T {
  return JSON.parse(decodeWeb64(part)) as T;
}

function decodeWeb64(encoded: string) {
  encoded = encoded.replace(/-/g, "+").replace(/_/g, "/");

  const padding = encoded.length % 4;
  switch (padding) {
    case 2:
      encoded = encoded + "==";
      break;
    case 3:
      encoded = encoded + "=";
      break;
  }

  return Buffer.from(encoded, "base64").toString("utf8");
}

// ====

export function encodePart(value: {
  [key: string]: string | undefined;
}): string {
  return encodeWeb64(JSON.stringify(value));
}

function encodeWeb64(value: string) {
  const encoded = Buffer.from(value, "utf8")
    .toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_");

  // remove padding

  if (encoded.endsWith("==")) {
    return encoded.substr(0, encoded.length - 2);
  }

  if (encoded.endsWith("=")) {
    return encoded.substr(0, encoded.length - 1);
  }

  return encoded;
}

// ====

/** Unix time in seconds */
export function now() {
  return +new Date() / 1000;
}
