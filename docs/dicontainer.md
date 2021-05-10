# DI integration

Loadit allows for doing dependency injection in test files using the "Class" approach:

```c#
return await Execute.Run<Test2>();

public class Test2 : LoadTest
{
    private readonly HttpClient _http;
    private readonly YourService _service;

    public Test2(HttpClient http, YourService service)
    {
        _http = http;
        _service = service;
    }
    
    //removed
}
```

To register your own services modify the "Startup.cs" file and register services in the DI container:

```c#
public class Startup : IStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        //Add your own services if required
        services.AddSingleton<YourService>();
    }
}
```

*Note:* Remember to add your custom services to the *.lib project or they will not be compiled into the test project.

![DI Container](img\di-container.png)