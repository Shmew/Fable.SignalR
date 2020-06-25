const path = require("path");
const testsDir = path.join(__dirname, "../../dist/tests");

module.exports = {
    allFiles: true,
    entry: path.join(__dirname, "./Fable.SignalR.Tests.fsproj"),
    outDir: testsDir,
    babel: {
        plugins: [
            "@babel/plugin-transform-modules-commonjs"
        ],
        sourceMaps: "inline"
    },
    onCompiled() {
        const fs = require('fs')
        const findSnapshotLoader = () => {
            const jesterDir =
                fs
                    .readdirSync(testsDir)
                    .sort()
                    .reverse()
                    .find(item => { return item.startsWith("Fable.Jester") })

            return require(path.join(testsDir, jesterDir, "SnapshotLoader"))
        }

        findSnapshotLoader().copySnaps(__dirname, this.outDir)
    }
};