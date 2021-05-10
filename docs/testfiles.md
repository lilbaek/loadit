# Test files

Each *.cs file in your test project should contain one load test. It is possible to define the load test in two different ways.

## Func test

The "Func test" is a quick way to get up and running with minimal code:

```c#
return await Execute.Run<HttpClient>(options =>
{
    options.VUs = 1;
    options.Iterations = 10;
}, async (http, token) =>
{
    using var res = await http.GetAsync("https://test.loadit.dev/", token);
});
```

The code inside the async block will be executed in parallel depending on the number of VUs.

## Class test

The "Class test" is slightly more expressive but allows for Setup and Teardown logic to get executed.

```c#
return await Execute.Run<Test2>();

public class Test2 : LoadTest
{
    private readonly HttpClient _http;

    public Test2(HttpClient http)
    {
        _http = http;
    }
    
    public override LoadOptions Options()
    {
        return new()
        {
            VUs = 2,
            Iterations = 2
        };
    }

    public override Task Setup(CancellationToken token)
    {
        //Run setup code
        return Task.CompletedTask;
    }

    public override async Task Run(CancellationToken token)
    {
        //Run in parallel based on VUs
        using var res = await _http.GetAsync("https://test.loadit.dev/");
    }

    public override Task Teardown(CancellationToken token)
    {
        //Run tear down
        return Task.CompletedTask;
    }
}
```

- The code inside the "Setup" method is run once before the start of the test
- The code inside the "Run" method will be executed in parallel depending on the number of VUs.
- The code inside the "Teardown" method is run once after the test is done