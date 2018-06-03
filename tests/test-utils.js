// @ts-check

const { request } = require("http");

/**
 * @return {Promise<{statusCode:number,statusMessage:string,headers:any,content:string}>}
 */
function requestAsync(options) {
  return new Promise((resolve, reject) => {
    const req = request(options, res => {
      const { statusCode, statusMessage, headers } = res;
      res.setEncoding("utf8");
      let content = "";
      res.on("data", chunk => (content += chunk));
      res.on("end", () => {
        resolve({ statusCode, statusMessage, headers, content });
      });
    });
    req.on("error", reject);
    req.end();
  });
}

module.exports = {
  requestAsync
};
