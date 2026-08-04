using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;

namespace Contoso.Notes.Api.Tests.TestSupport;

/// <summary>
/// Minimal FunctionContext for unit tests. Most members are unused by the
/// HTTP trigger path under test; the worker's runtime supplies real values
/// in production. Kept deliberately tiny so tests do not need Moq for one
/// assertion.
/// </summary>
internal sealed class FakeFunctionContext : FunctionContext
{
    // Building the worker's default host is the canonical way to wire the
    // ObjectSerializer the worker resolves for WriteAsJsonAsync; the worker's
    // GetService<ObjectSerializer>() expects the exact Type the runtime binds
    // to, which is unifiable only when the host has registered it. The host
    // also gives us a real WorkerOptions for free.
    private static readonly ServiceProvider Services = BuildServices();

    private static ServiceProvider BuildServices()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .Build();
        return (ServiceProvider)host.Services;
    }

    public override string InvocationId => "test-invocation";
    public override string FunctionId => "notes";
    public override TraceContext TraceContext => null!;
    public override BindingContext BindingContext => null!;
    public override RetryContext RetryContext => null!;
    public override IServiceProvider InstanceServices { get; set; } = Services;
    public override FunctionDefinition FunctionDefinition => null!;
    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
    public override IInvocationFeatures Features => null!;
}

/// <summary>
/// Minimal HttpRequestData for unit tests. Returns a memory-backed response
/// whose body the test can read back.
/// </summary>
internal sealed class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext)
        : base(functionContext) { }

    public override Stream Body => Stream.Null;
    public override HttpHeadersCollection Headers { get; } = new();
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => new("http://localhost/api/notes");
    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();
    public override string Method => "GET";

    public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
}

internal sealed class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext functionContext)
        : base(functionContext) => Body = new MemoryStream();

    public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = Stream.Null;
    public override HttpCookies Cookies => null!;
}
