#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using explainpowershell.models;
using ExplainPowershell.SyntaxAnalyzer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ExplainPowershell.SyntaxAnalyzer.Tests
{
    [TestFixture]
    public class SyntaxAnalyzerFunctionTests
    {
        [Test]
        public async Task Run_DoesNotRequireAzureWebJobsStorage_WhenHelpRepositoryInjected()
        {
            Environment.SetEnvironmentVariable("AzureWebJobsStorage", null);

            var loggerFactory = LoggerFactory.Create(_ => { });
            var logger = loggerFactory.CreateLogger<SyntaxAnalyzerFunction>();

            var helpRepository = new InMemoryHelpRepository();
            var function = new SyntaxAnalyzerFunction(logger, helpRepository);

            var request = new Code { PowershellCode = "Get-Process" };
            var bodyJson = JsonSerializer.Serialize(request);

            var req = new TestHttpRequestData(bodyJson);
            var resp = await function.Run(req);

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            resp.Body.Position = 0;
            using var reader = new StreamReader(resp.Body, Encoding.UTF8);
            var responseJson = await reader.ReadToEndAsync();

            var analysisResult = JsonSerializer.Deserialize<AnalysisResult>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(analysisResult, Is.Not.Null);
            Assert.That(analysisResult!.ExpandedCode, Is.Not.Empty);
        }

        private sealed class TestHttpRequestData : HttpRequestData
        {
            private readonly MemoryStream body;
            private readonly HttpHeadersCollection headers = new();
            private readonly Uri url;

            public TestHttpRequestData(string bodyJson)
                : base(new TestFunctionContext())
            {
                body = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
                url = new Uri("http://127.0.0.1/api/SyntaxAnalyzer");
            }

            public override Stream Body => body;

            public override HttpHeadersCollection Headers => headers;

            public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

            public override Uri Url => url;

            public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();

            public override string Method => "POST";

            public override HttpResponseData CreateResponse()
            {
                return new TestHttpResponseData(FunctionContext);
            }
        }

        private sealed class TestHttpResponseData : HttpResponseData
        {
            public TestHttpResponseData(FunctionContext functionContext)
                : base(functionContext)
            {
                Headers = new HttpHeadersCollection();
                Body = new MemoryStream();
                Cookies = new TestHttpCookies();
            }

            public override HttpStatusCode StatusCode { get; set; }

            public override HttpHeadersCollection Headers { get; set; }

            public override Stream Body { get; set; }

            public override HttpCookies Cookies { get; }
        }

        private sealed class TestHttpCookies : HttpCookies
        {
            private readonly Dictionary<string, IHttpCookie> cookies = new(StringComparer.OrdinalIgnoreCase);

            public override void Append(IHttpCookie cookie)
            {
                cookies[cookie.Name] = cookie;
            }

            public override void Append(string name, string value)
            {
                cookies[name] = new TestHttpCookie { Name = name, Value = value };
            }

            public override IHttpCookie CreateNew()
            {
                return new TestHttpCookie();
            }

            private sealed class TestHttpCookie : IHttpCookie
            {
                public string Name { get; set; } = string.Empty;
                public string Value { get; set; } = string.Empty;
                public string? Domain { get; set; }
                public string? Path { get; set; }
                public DateTimeOffset? Expires { get; set; }
                public bool? Secure { get; set; }
                public bool? HttpOnly { get; set; }
                public SameSite SameSite { get; set; }
                public double? MaxAge { get; set; }
            }
        }

        private sealed class TestFunctionContext : FunctionContext
        {
            private IDictionary<object, object> items = new Dictionary<object, object>();

            public override string InvocationId { get; } = Guid.NewGuid().ToString("n");

            public override string FunctionId { get; } = "TestFunction";

            public override TraceContext TraceContext => throw new NotImplementedException();

            public override BindingContext BindingContext => throw new NotImplementedException();

            public override RetryContext RetryContext => null!;

            public override IServiceProvider InstanceServices
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            public override FunctionDefinition FunctionDefinition => throw new NotImplementedException();

            public override IDictionary<object, object> Items
            {
                get => items;
                set => items = value;
            }

            public override IInvocationFeatures Features => throw new NotImplementedException();
        }
    }
}
