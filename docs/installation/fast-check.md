# Fable.FastCheck

To install `Fable.FastCheck` you need to add the 
nuget package into your F# project:

```bash
# nuget
dotnet add package Fable.FastCheck
# paket
paket add Fable.FastCheck --project ./project/path
```
Then you need to install the corresponding npm dependencies.
```bash
npm install fast-check --save-dev

___

yarn add fast-check --dev
```

### Use Femto

If you happen to use [Femto], then it can 
install everything for you in one go:

```bash
cd ./project
femto install Fable.FastCheck
```
Here, the nuget package will be installed 
using the package manager that the project 
is using (detected by Femto) and then the 
required npm packages will be resolved

[Femto]: https://github.com/Zaid-Ajaj/Femto
