# Fable.ReactTestingLibrary

To install `Fable.ReactTestingLibrary` you need 
to add the nuget package into your F# project:

```bash
# nuget
dotnet add package Fable.ReactTestingLibrary
# paket
paket add Fable.ReactTestingLibrary --project ./project/path
```
Then you need to install the corresponding npm dependencies.
```bash
npm install @testing-library/react --save-dev
npm install @testing-library/user-event --save-dev

npm install @babel/plugin-transform-modules-commonjs --save-dev // Recommended, but not necessary
___

yarn add @testing-library/react --dev
yarn add @testing-library/user-event --dev

yarn add @babel/plugin-transform-modules-commonjs --dev // Recommended, but not necessary
```

This library does not need the main package 
to function, so it is possible to use with 
[Fable.Mocha].

Do note that using this library standalone 
will mean you have no access to any `expect` 
methods that are commonly used with it.

### Use Femto

If you happen to use [Femto], then it can 
install everything for you in one go:

```bash
cd ./project
femto install Fable.ReactTestingLibrary
```
Here, the nuget package will be installed using the package manager that the project is using (detected by Femto) and then the required npm packages will be resolved

Do note that this will *not* install the optional dependencies listed above (the babel plugin).


[Fable.Mocha]: https://github.com/Zaid-Ajaj/Fable.Mocha
[Femto]: https://github.com/Zaid-Ajaj/Femto
