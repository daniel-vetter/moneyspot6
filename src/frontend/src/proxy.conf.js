const target = process.env["services__Backend__http__0"] || "http://localhost:5182";

module.exports = {
  "/api": {
    target,
    secure: false,
    ws: true,
  },
  "/signin-oidc": {
    target,
    secure: false,
  },
};
