# Project structure
Loadit uses the C# 9 top-level programs that allow a very intuitive, simple and streamlined experience for creating tests.
Normally you can only have one top-level programs in a project. Loadit workaround this by including/excluding *.cs files on a project level.
As all *.cs are excluded by default and you will need to keep any shared code in the *.lib project.

To create a new project with the recommended setup use the `new` command.

```bash
loadit --new --name load-tests
```

## Startup.cs
A single file called `Startup.cs` is compiled together with the test getting run.
The `Startup` class allows you to register/re-register your own services in the DI Container used by Loadit.

## Appsettings.json

The appsettings.json file allows you to change/modify the settings globally for all tests in a project.