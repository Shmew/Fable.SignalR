### 0.11.6 - tbd
* Add .NET 6 support

### 0.11.5 - Tuesday, June 29th, 2021
* Update client side JSON serialization to remove warnings in Fable 3

### 0.11.4 - Wednesday, April 7th, 2021
* MsgPack optimizations

### 0.11.3 - Friday, March 19th, 2021
* Fix nuget package dependency restrictions

### 0.11.2 - Friday, January 22nd, 2021
* Fix framework targeting

### 0.11.1 - Monday, December 28th, 2020
* Make ConfigBuilder Build method public

### 0.11.0 - Friday, November 20th, 2020
* Support FSharp.Core 5.0
* Add .NET 5 support

### 0.10.1 - Friday, October 23rd, 2020
* Add target netstandard for dotnet client

### 0.10.0 - Friday, October 23rd, 2020
* Fix MsgPack protocol issue
* Add support for using the ISignalRServerBuilder

### 0.9.0 - Friday, October 23rd, 2020
* Added support for the .NET client

### 0.8.3 - Tuesday, October 13th, 2020
* Fix issue with MsgPack protocol handling when messages
are batched

### 0.8.2 - Monday, October 12th, 2020
* Pin Fable.SimpleJson
* Use Fable.Remoting.MsgPack for serialization

### 0.8.1 - Sunday, September 20th, 2020
* Fix Fable compilation

### 0.8.0 - Sunday, September 20th, 2020
* Add message pack support

### 0.7.1 - Thursday, September 17th, 2020
* Fix femto configuration

### 0.7.0 - Thursday, September 17th, 2020
* Added support for getting hub context via DI

### 0.6.2 - Thursday, September 2nd, 2020
* Update dependencies

### 0.6.1 - Wednesday, August 12th, 2020
* Fix routing middleware not being applied if a config is not given

### 0.6.0 - Tuesday, August 11th, 2020
* Add support for authorizatation
* Add websocket middleware to support bearer authentication

### 0.5.0 - Tuesday, July 28th, 2020
* Expose more of the hub context for invocations

### 0.4.1 - Thursday, July 23rd, 2020
* Fix an issue with invocation caller resolution

### 0.4.0 - Friday, July 10th, 2020
* Make invoke server api asynchronous - thanks @Prunkles

### 0.3.0 - Friday, July 3rd, 2020
* Add support for dependency injection in hub functions

### 0.2.0 - Thursday, July 2nd, 2020
* Remove ISubscription infavor of System.IDisposable casting
* Add SignalR.logger to make creating an ILogger easier

### 0.1.0 - Wednesday, July 1st, 2020
* Initial release
