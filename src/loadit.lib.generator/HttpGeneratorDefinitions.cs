using System.Collections.Generic;

namespace Loadit.Analyzer
{
    /*
    internal static class HttpGeneratorDefinitions
    {
        internal static List<HttpGenerateMethodEntry> Generate = new();

        static HttpGeneratorDefinitions()
        {
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "GetAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"requestUri", "string?"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "Uri?"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "string?"},
                        {"completionOption", "HttpCompletionOption"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "PostAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"requestUri", "string?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "Uri?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "PutAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"requestUri", "string?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "Uri?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "PatchAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"requestUri", "string?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "Uri?"},
                        {"content", "HttpContent"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "DeleteAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"requestUri", "string?"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"requestUri", "Uri?"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
            Generate.Add(new HttpGenerateMethodEntry()
            {
                MethodName = "SendAsync",
                ResponseType = "Task<HttpResponseMessage?>",
                Variations = new List<Dictionary<string, string>>()
                {
                    new()
                    {
                        {"request", "HttpRequestMessage"},
                        {"cancellationToken", "CancellationToken"}
                    },
                    new()
                    {
                        {"request", "HttpRequestMessage"},
                        {"completionOption", "HttpCompletionOption"},
                        {"cancellationToken", "CancellationToken"}
                    }
                }
            });
        }
    }

    internal class HttpGenerateMethodEntry
    {
        public string MethodName { get; set; } = null!;
        public string ResponseType { get; set; } = null!;
        public List<Dictionary<string, string>> Variations { get; set; } = new();
    }*/
}