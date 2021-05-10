# Quick start

On Linux/Mac/Windows invoke the dotnet tool command:

```bash
dotnet tool install -g loadit
```

*Install [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0) first*

## Initialize

If you want to write your tests in the `./load-tests` subdirectory, you can use the `new` command.

```bash
loadit --new --name load-tests
```

## Writing tests

After the `new` command is complete, you can see the new project in the `./load-tests` subdirectory.

* `load-tests.sln` solution file for loading in Visual Studio/Rider/VS Code
* `load-tests` project for writing tests
* `load-tests.lib` project for sharing code between tests
* `load-tests/Test1.cs` the most simple test possible
* `load-tests/Test2.cs` more options including the option for setup/teardown logic

You can easily add new tests by creating a new *.cs file in the load-tests subdirectory.

## Run tests

Run a test using `loadit --run`. 

```bash
loadit --run --file load-tests\load-tests\Test1.cs
```

## Sharing code

Loadit uses the C# 9 top-level programs that allow a very intuitive, simple and streamlined experience for creating tests.
Normally you can only have one top-level programs in a project. Loadit workaround this by including/excluding *.cs files on a project level.
As all *.cs are excluded by default and you will need to keep any shared code in the *.lib project.

See the [Project structure](structure) for more details

## Upgrading the CLI

Upgrade to the latest version using `dotnet tool update`. 

```bash
dotnet tool update loadit -g
```
