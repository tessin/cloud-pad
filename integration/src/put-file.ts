import https = require("https");
import url = require("url");

import { OutgoingHttpHeaders, IncomingHttpHeaders } from "http";
import { createReadStream, statSync } from "fs";

export type FileResponse = {
  statusCode: number;
  statusMessage: string;
  headers: IncomingHttpHeaders;
};

export async function putFile(
  method: string,
  request: string,
  filename: string,
  headers?: OutgoingHttpHeaders
) {
  return new Promise<FileResponse>((resolve, reject) => {
    const headers2: OutgoingHttpHeaders = { ...headers };

    const contentLength = fileSize(filename);
    headers2["content-length"] = contentLength;

    const req = https.request(
      { method: method, ...url.parse(request), headers: headers2 },
      res => {
        resolve({
          statusCode: res.statusCode!,
          statusMessage: res.statusMessage!,
          headers: res.headers
        });
      }
    );

    req.on("error", reject);

    createReadStream(filename).pipe(req);
  });
}

function fileSize(path: string): number {
  try {
    return statSync(path).size;
  } catch {
    return 0;
  }
}
