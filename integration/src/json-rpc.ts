import https = require("https");
import url = require("url");

import { IncomingHttpHeaders, OutgoingHttpHeaders } from "http";

export class JsonRpcError extends Error {
  statusCode: number;
  statusMessage: string;
  headers: IncomingHttpHeaders;
  text: string;

  constructor(
    statusCode: number,
    statusMessage: string,
    headers: IncomingHttpHeaders,
    text: string
  ) {
    super("cannot parse message body");

    this.statusCode = statusCode;
    this.statusMessage = statusMessage;
    this.headers = headers;
    this.text = text;
  }
}

export type JsonRpcResponse<T> = {
  statusCode: number;
  statusMessage: string;
  headers: IncomingHttpHeaders;
  value: T;
};

export function jsonRpc<T>(
  method: string,
  request: string,
  body?: any,
  headers?: OutgoingHttpHeaders
) {
  return new Promise<JsonRpcResponse<T>>((resolve, reject) => {
    const headers2: OutgoingHttpHeaders = { ...headers };

    let buffer: Buffer | undefined;

    if (body !== undefined) {
      buffer = Buffer.from(JSON.stringify(body));

      headers2["content-type"] = "application/json; charset=utf-8";
      headers2["content-length"] = buffer.length;
    }

    headers2["accept"] = "application/json";

    const req = https.request(
      { method: method, ...url.parse(request), headers: headers2 },
      res => {
        res.setEncoding("utf8");

        res.on("error", reject);

        let text = "";

        res.on("data", chunk => {
          text += chunk;
        });

        res.on("end", () => {
          let value: T | undefined;
          try {
            value = JSON.parse(text) as T;
          } catch (err) {
            reject(
              new JsonRpcError(
                res.statusCode!,
                res.statusMessage!,
                res.headers,
                text
              )
            );
            return;
          }
          resolve({
            statusCode: res.statusCode!,
            statusMessage: res.statusMessage!,
            headers: res.headers,
            value
          });
        });
      }
    );

    req.on("error", reject);

    if (buffer) {
      req.end(buffer);
    } else {
      req.end();
    }
  });
}
