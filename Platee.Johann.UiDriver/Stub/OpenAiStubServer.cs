namespace Platee.Johann.UiDriver.Stub;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

public sealed class OpenAiStubServer : IDisposable
{
    private readonly HttpListener listener = new();
    private readonly ConcurrentQueue<StubRequest> requests = new();
    private readonly ConcurrentQueue<Exception> errors = new();
    private readonly ConcurrentDictionary<string, int> failNext = new();
    private readonly ConcurrentDictionary<string, bool> hanging = new();
    private readonly ConcurrentDictionary<string, bool> missingModels = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Task, byte> inFlight = new();
    private readonly CancellationTokenSource stop = new();
    private Func<string, string?> chat = _ => null;
    private Func<string, string?> transcription = _ => null;

    private OpenAiStubServer(int port)
    {
        this.Root = new Uri($"http://localhost:{port}/");
        this.listener.Prefixes.Add(this.Root.AbsoluteUri);
    }

    public Uri Root { get; }

    public IReadOnlyList<StubRequest> Requests => this.requests.ToArray();

    public IReadOnlyList<Exception> Errors => this.errors.ToArray();

    public static OpenAiStubServer Start()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var server = new OpenAiStubServer(port);
        server.listener.Start();
        _ = Task.Run(server.LoopAsync);
        return server;
    }

    public void OnChat(Func<string, string?> responder) => this.chat = responder;

    public void OnTranscription(Func<string, string?> responder) => this.transcription = responder;

    public void FailNext(string pathPrefix, int status) => this.failNext[pathPrefix] = status;

    public void Hang(string pathPrefix) => this.hanging[pathPrefix] = true;

    public void MissingModel(string modelId) => this.missingModels[modelId] = true;

    public void Dispose()
    {
        this.stop.Cancel();
        this.listener.Close();

        try
        {
            Task.WhenAll(this.inFlight.Keys).Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Handlers unwind via the cancelled token or a closed listener during shutdown;
            // any exception has already been recorded in this.errors where relevant.
        }
    }

    private async Task LoopAsync()
    {
        while (!this.stop.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await this.listener.GetContextAsync();
            }
            catch (Exception) when (this.stop.IsCancellationRequested)
            {
                return;
            }

            var handler = Task.Run(() => this.HandleAsync(ctx));
            this.inFlight[handler] = 0;
            _ = handler.ContinueWith(t => this.inFlight.TryRemove(t, out _), TaskScheduler.Default);
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            await this.HandleRequestAsync(ctx);
        }
        catch (Exception) when (this.stop.IsCancellationRequested)
        {
            // Caused by our own shutdown (cancellation, or the listener/response closed
            // right after Dispose ran) — not a stub bug, so it is not recorded as an error.
        }
        catch (Exception ex)
        {
            this.errors.Enqueue(ex);
            await TryWriteError(ctx, ex);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;
        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
        var body = await reader.ReadToEndAsync();
        var fileName = path.EndsWith("/audio/transcriptions", StringComparison.Ordinal)
            ? MultipartFileName(body)
            : null;
        this.requests.Enqueue(new StubRequest(ctx.Request.HttpMethod, path, body, fileName));

        var hang = this.hanging.Keys.FirstOrDefault(p => path.StartsWith(p, StringComparison.Ordinal));
        if (hang is not null)
        {
            await Task.Delay(Timeout.Infinite, this.stop.Token).ContinueWith(_ => { });
            return;
        }

        var fail = this.failNext.Keys.FirstOrDefault(p => path.StartsWith(p, StringComparison.Ordinal));
        if (fail is not null && this.failNext.TryRemove(fail, out var status))
        {
            await Write(ctx, status, """{"error":{"message":"stub failure","type":"server_error"}}""");
            return;
        }

        if (path.StartsWith("/v1/models/", StringComparison.Ordinal))
        {
            var id = Uri.UnescapeDataString(path["/v1/models/".Length..]);
            await (this.missingModels.ContainsKey(id)
                ? Write(ctx, 404, """{"error":{"message":"model not found","type":"invalid_request_error"}}""")
                : Write(ctx, 200, JsonSerializer.Serialize(new { id, @object = "model", created = 0, owned_by = "stub" })));
            return;
        }

        if (path == "/v1/chat/completions")
        {
            var user = JsonNode.Parse(body)?["messages"]?.AsArray().LastOrDefault()?["content"]?.GetValue<string>() ?? string.Empty;
            var text = this.chat(user);
            await (text is null
                ? Write(ctx, 500, $"kein Stub für Chat-Anfrage: {Shorten(user)}")
                : Write(ctx, 200, ChatCompletion(text)));
            return;
        }

        if (path == "/v1/audio/transcriptions")
        {
            var text = this.transcription(fileName ?? string.Empty);
            await (text is null
                ? Write(ctx, 500, $"kein Stub für Transkription: {fileName}")
                : Write(ctx, 200, JsonSerializer.Serialize(new { text })));
            return;
        }

        await Write(ctx, 404, $"kein Stub für {ctx.Request.HttpMethod} {path}");
    }

    private static string ChatCompletion(string text) => JsonSerializer.Serialize(new
    {
        id = "chatcmpl-stub",
        @object = "chat.completion",
        created = 0,
        model = "stub",
        choices = new[] { new { index = 0, message = new { role = "assistant", content = text }, finish_reason = "stop" } },
        usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 },
    });

    private static string? MultipartFileName(string body)
    {
        const string marker = "filename=";
        var i = body.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }

        var rest = body[(i + marker.Length)..].TrimStart('"');
        var end = rest.IndexOfAny(['"', '\r', '\n', ';']);
        return end < 0 ? rest : rest[..end];
    }

    private static string Shorten(string s) => s.Length <= 80 ? s : s[..80] + "…";

    private static async Task TryWriteError(HttpListenerContext ctx, Exception ex)
    {
        try
        {
            await Write(ctx, 500, JsonSerializer.Serialize(new { error = new { message = ex.Message, type = "stub_error" } }));
        }
        catch (Exception)
        {
            // The response stream may already be closed or the connection gone; nothing more
            // we can do to report it to the caller. It is still recorded in Errors.
        }
    }

    private static async Task Write(HttpListenerContext ctx, int status, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = body.StartsWith('{') ? "application/json" : "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }
}
