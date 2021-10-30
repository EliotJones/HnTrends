// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

// https://stackoverflow.com/questions/58083510/best-way-to-integrate-webpack-builds-with-asp-net-core-3-0

var path = require("path");

const webpackDevServerPort = 6483;
const proxyTarget = "http://localhost:5225";

module.exports = {
  mode: "development",
  entry: "./src/App.fs.js",
  output: {
    path: path.join(__dirname, "../wwwroot/js"),
    filename: "bundle.js",
  },
  devServer: {
    publicPath: "/",
    contentBase: "../wwwroot/js",
    writeToDisk: true,
    compress: true,
    proxy: {
      "*": {
        target: proxyTarget,
      },
    },
    port: webpackDevServerPort,
  },
  module: {},
};
