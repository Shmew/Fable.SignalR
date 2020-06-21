# Fable.Jester

To install `Fable.Jester` to add the nuget 
package into your F# project:

```bash
# nuget
dotnet add package Fable.Jester
# paket
paket add Fable.Jester --project ./project/path
```
Then you need to install the corresponding npm dependencies.
```bash
npm install jest --save-dev
npm install @testing-library/jest-dom --save-dev

npm install @babel/plugin-transform-modules-commonjs --save-dev // Recommended, but not necessary
npm install @sinonjs/fake-timers --save-dev // If you plan to use timer mocks
npm install prettier --save-dev // If you plan to use snapshot testing
___

yarn add jest --dev
yarn add @testing-library/jest-dom --dev

yarn add @babel/plugin-transform-modules-commonjs --dev // Recommended, but not necessary
yarn add @sinonjs/fake-timers --dev // If you plan to use timer mocks
yarn add prettier --dev // If you plan to use snapshot testing
```

### Use Femto

If you happen to use [Femto], then it can 
install everything for you in one go:

```bash
cd ./project
femto install Fable.Jester
```
Here, the nuget package will be installed 
using the package manager that the project 
is using (detected by Femto) and then the 
required npm packages will be resolved

Do note that this will *not* install the 
optional dependencies listed above (the 
babel plugin and prettier).

[Femto]: https://github.com/Zaid-Ajaj/Femto
