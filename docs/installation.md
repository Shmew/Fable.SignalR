# Fable.SignalR

## Fable

To install the Fable client first add the `Fable.SignalR` 
nuget package into your F# project:

```bash
# nuget
dotnet add package Fable.SignalR

dotnet add package Fable.SignalR.Elmish // For Elmish Cmds
dotnet add package Fable.SignalR.Feliz // For Feliz hooks

# paket
paket add Fable.SignalR --project ./project/path

paket add Fable.SignalR.Elmish --project ./project/path // For Elmish Cmds
paket add Fable.SignalR.Feliz --project ./project/path // For Feliz hooks
```

Then you need to install the corresponding npm dependencies.
```bash
npm install @microsoft/signalr
___

yarn add @microsoft/signalr
```

### Use Femto

If you happen to use [Femto], then it can 
install everything for you in one go:

```bash
cd ./project
femto install Fable.SignalR

femto install Fable.SignalR.Elmish // For Elmish Cmds
femto install Fable.SignalR.Feliz // For Feliz hooks
```
Here, the nuget package will be installed 
using the package manager that the project 
is using (detected by Femto) and then the 
required npm packages will be resolved

[Femto]: https://github.com/Zaid-Ajaj/Femto

## On the Server

To install on the server install one of the following 
nuget packages into your F# project:

* Fable.SignalR.AspNetCore
 - For both native ASP.NET Core and Giraffe servers 
 (there is no direct dependencies on Giraffe).
* Fable.SignalR.Saturn
 - Saturn CE extensions and Saturn style CE's for hub configuration.

```bash
# nuget
dotnet add package Fable.SignalR.AspNetCore // For ASP.NET Core or Giraffe
dotnet add package Fable.SignalR.Saturn // For Saturn

# paket
paket add Fable.SignalR.AspNetCore --project ./project/path // For ASP.NET Core or Giraffe
paket add Fable.SignalR.Saturn --project ./project/path // For Saturn
```

## .NET Client

To install for .NET clients add one (or both) of the following
nuget packages into your F# project:

* Fable.SignalR.DotNet
* Fable.SignalR.DotNet.Elmish
 - If you want to use Elmish Cmds.

```bash
# nuget
dotnet add package Fable.SignalR.DotNet
dotnet add package Fable.SignalR.DotNet.Elmish 

# paket
paket add Fable.SignalR.DotNet --project ./project/path
paket add Fable.SignalR.DotNet.Elmish --project ./project/path
```
