# Simple.LocalDb

Simple.LocalDb is a simple managed wrapper around the LocalDB API.

This framework has no features. It does not have error checking. It does not do anything smart. It doesn't try to help you in any way. There are no unit tests. There are no integration tests. There are no interfaces, inheritance, coupling, or clever abstractions.

If you're like me and you basically want to call 'CreateInstance' from your test code, this package is your friend.

If you need to automagically deploy a dacpac to a dynamically created test instance, this is not your package.

If you want a smart and robust LocalDb provider that can create different versions of instances, be localized, will clean up cruft localdb files from the filesystem, and will recover from failures, this is not your package.

Try one of these instead:

 - [Nicholas Ham's LocalDb NuGet package](https://www.nuget.org/packages/SqlLocalDb.Dac/).
 -  [Martin Costello's  LocalDb NuGet package](https://www.nuget.org/packages/System.Data.SqlLocalDb/).

To use this package:

- You can create an instance of the managed interface, which hides some of the marshaling complexity from you (there's not much).

- You can create an instance of the unmanaged interface and write your own better wrapper around it.

- If you want to use it as a nuget package. Have at it.

- If you want to include the project source in your project. Have at it.

- If you want to copy paste parts of the code into yours. Have at it.

- If you need to target the .Net 3.5 (neee 2.0) runtime and reference a signed assembly. Grab the source and have at it.
- If you want to copy this code and publish a new nuget package from it with a different name and 0..n modifications. Have at it.

Example Managed API usage:

```
//New up the API.
var api = new ManagedLocalDbApi();

//Get all the instances as a list of strings.
var instanceNames = api.GetInstanceNames().ToList();

//Create a new instance with a random name.
var testInstanceName = Path.GetRandomFileName();
api.CreateInstance(testInstanceName);

//Start the instance and get its connection string (named pipes).
var connection = api.StartInstance(testInstanceName);

//Get a bunch of fun details about the instance.
var testInstance = api.GetInstance(testInstanceName);

//Stop the instance. Timeout if it takes longer than 60 seconds.
api.StopInstance(testInstanceName, TimeSpan.FromSeconds(60));

//Delete the instance.
api.DeleteInstance(testInstanceName);
```


