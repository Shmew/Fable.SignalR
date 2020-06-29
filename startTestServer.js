const { spawn } = require('child_process')

const runServer = () => {
    console.log("Starting server...")

    const ps = spawn('dotnet', ['run', '-p', './tests/Fable.SignalR.TestServer/Fable.SignalR.TestServer.fsproj'])

    ps.stdout.on('data', (data) => {
        console.log(data.toString())
    })

    ps.stderr.on('data', (data) => {
        console.error(data.toString())
    })

    process.on('exit', () => {
        ps.kill()
    })

    process.on('SIGINT', () => ps.kill())
    process.on('SIGTERM', () => ps.kill())
}

runServer()
