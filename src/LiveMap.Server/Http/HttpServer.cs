using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Owin;
using static CitizenFX.Core.Native.API;

namespace LiveMap.Server.Http;

internal class HttpServer : IServer
{
    private static HttpServerScript _mTicker;

    public HttpServer()
    {
        _mTicker = new HttpServerScript();

        BaseScript.RegisterScript(_mTicker);
    }

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public void Dispose()
    {
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        SetHttpHandler(new Action<dynamic, dynamic>(async (req, res) =>
        {
            var resourceName = GetCurrentResourceName();

            var bodyStream = req.method != "GET" && req.method != "HEAD"
                ? await GetBodyStream(req)
                : Stream.Null;

            var oldSc = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            var cts = new CancellationTokenSource();
            req.setCancelHandler(new Action(() => { cts.Cancel(); }));

            await Task.Factory.StartNew(async () =>
            {
                var owinEnvironment = new Dictionary<string, object>
                {
                    ["owin.RequestBody"] = bodyStream
                };

                var headers = new HeaderDictionary();

                foreach (var headerPair in req.headers)
                    headers.Add(headerPair.Key, new string[] { headerPair.Value.ToString() });

                owinEnvironment["owin.RequestHeaders"] = headers;

                owinEnvironment["owin.RequestMethod"] = req.method;
                owinEnvironment["owin.RequestPath"] = req.path.Split('?')[0];
                owinEnvironment["owin.RequestPathBase"] = "/" + resourceName;
                owinEnvironment["owin.RequestProtocol"] = "HTTP/1.1";
                owinEnvironment["owin.RequestQueryString"] = req.path.Contains('?') ? req.path.Split('?', 2)[1] : "";
                owinEnvironment["owin.RequestScheme"] = "http";

                var outStream = new HttpOutStream(owinEnvironment, res);
                owinEnvironment["owin.ResponseBody"] = outStream;

                var outHeaders = new Dictionary<string, string[]>();
                owinEnvironment["owin.ResponseHeaders"] = outHeaders;

                owinEnvironment["owin.CallCancelled"] = cts.Token;
                owinEnvironment["owin.Version"] = "1.0";

                var ofc = new FxOwinFeatureCollection(owinEnvironment);
                var context = application.CreateContext(new FeatureCollection(ofc));

                try
                {
                    await application.ProcessRequestAsync(context);
                    await ofc.InvokeOnStarting();
                }
                catch (Exception ex)
                {
                    await ofc.InvokeOnCompleted();

                    application.DisposeContext(context, ex);

                    var errorText = "Error."u8.ToArray();

                    owinEnvironment["owin.ResponseStatusCode"] = 500;
                    await outStream.WriteAsync(errorText, 0, errorText.Length);
                    await outStream.EndStream();

                    return;
                }

                application.DisposeContext(context, null);

                await outStream.EndStream();

                await ofc.InvokeOnCompleted();
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            SynchronizationContext.SetSynchronizationContext(oldSc);
        }));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public static Task QueueTick(Action action)
    {
        var tcs = new TaskCompletionSource<int>();

        _mTicker.TickQueue.Enqueue(Tuple.Create(action, tcs));

        ScheduleResourceTick(ServerMain.Self.ResourceName);

        return tcs.Task;
    }

    private static async Task<Stream> GetBodyStream(dynamic req)
    {
        var tcs = new TaskCompletionSource<byte[]>();

        req.setDataHandler(new Action<byte[]>(data => { tcs.SetResult(data); }), "binary");

        var bytes = await tcs.Task;

        return new MemoryStream(bytes);
    }

    private class HttpServerScript : BaseScript
    {
        public ConcurrentQueue<Tuple<Action, TaskCompletionSource<int>>> TickQueue { get; } = new();

        [Tick]
        public Task OnTick()
        {
            while (TickQueue.TryDequeue(out var call))
                try
                {
                    call.Item1();

                    call.Item2.SetResult(0);
                }
                catch (Exception e)
                {
                    call.Item2.SetException(e);
                }

            return Task.CompletedTask;
        }
    }
}

internal class HttpOutStream(Dictionary<string, object> owinEnvironment, dynamic res) : Stream
{
    private bool _headersSent;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override void Flush()
    {
        FlushAsync().Wait();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return !_headersSent ? HttpServer.QueueTick(EnsureHeadersSent) : Task.CompletedTask;
    }

    private void EnsureHeadersSent()
    {
        if (_headersSent) return;

        _headersSent = true;

        var realOutHeaders = owinEnvironment["owin.ResponseHeaders"] as IDictionary<string, string[]>;

        res.writeHead(
            owinEnvironment.TryGetValue("owin.ResponseStatusCode", out var value)
                ? (int)value
                : 200,
            realOutHeaders?.ToDictionary(a => a.Key, a => a.Value));
    }

    public Task EndStream()
    {
        return HttpServer.QueueTick(() =>
        {
            EnsureHeadersSent();

            res.send();
        });
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count).Wait();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return HttpServer.QueueTick(() =>
        {
            EnsureHeadersSent();

            SendBuffer();
        });

        void SendBuffer()
        {
            var outBytes = new byte[count];

            Buffer.BlockCopy(buffer, offset, outBytes, 0, count);

            res.write(outBytes);
        }
    }
}

internal class FxOwinFeatureCollection(IDictionary<string, object> environment)
    : OwinFeatureCollection(environment), IHttpResponseFeature, IHttpRequestLifetimeFeature
{
    private readonly List<Func<Task>> _mOnCompleted = [];
    private readonly List<Func<Task>> _mOnStarting = [];

    void IHttpRequestLifetimeFeature.Abort()
    {
    }

    void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
    {
        _mOnStarting.Add(() => callback(state));
    }

    void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
    {
        _mOnCompleted.Add(() => callback(state));
    }

    public async Task InvokeOnCompleted()
    {
        foreach (var action in _mOnCompleted) await action();
    }

    public async Task InvokeOnStarting()
    {
        foreach (var action in _mOnStarting) await action();
    }
}

internal class HeaderDictionary : IDictionary<string, string[]>
{
    private readonly Dictionary<string, string[]> _mBackingDict = new();

    public string[] this[string key]
    {
        get => _mBackingDict[NormalizeKey(key)];
        set => _mBackingDict[NormalizeKey(key)] = value;
    }

    public ICollection<string> Keys => _mBackingDict.Keys;

    public ICollection<string[]> Values => _mBackingDict.Values;

    public int Count => _mBackingDict.Count;

    public bool IsReadOnly => false;

    public void Add(string key, string[] value)
    {
        _mBackingDict.Add(NormalizeKey(key), value);
    }

    public void Add(KeyValuePair<string, string[]> item)
    {
        _mBackingDict.Add(NormalizeKey(item.Key), item.Value);
    }

    public void Clear()
    {
        _mBackingDict.Clear();
    }

    public bool Contains(KeyValuePair<string, string[]> item)
    {
        return _mBackingDict.Contains(new KeyValuePair<string, string[]>(NormalizeKey(item.Key), item.Value));
    }

    public bool ContainsKey(string key)
    {
        return _mBackingDict.ContainsKey(NormalizeKey(key));
    }

    public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
    {
        return _mBackingDict.GetEnumerator();
    }

    public bool Remove(string key)
    {
        return _mBackingDict.Remove(NormalizeKey(key));
    }

    public bool Remove(KeyValuePair<string, string[]> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(string key, out string[] value)
    {
        return _mBackingDict.TryGetValue(NormalizeKey(key), out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _mBackingDict.GetEnumerator();
    }

    private static string NormalizeKey(string key)
    {
        return key.ToLower();
    }
}