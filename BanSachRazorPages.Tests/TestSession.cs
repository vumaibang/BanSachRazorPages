using Microsoft.AspNetCore.Http;

namespace BanSachRazorPages.Tests;

internal sealed class TestHttpAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> values = [];
    public string Id { get; } = Guid.NewGuid().ToString();
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => values.Keys;
    public void Clear() => values.Clear();
    public void Remove(string key) => values.Remove(key);
    public void Set(string key, byte[] value) => values[key] = value;
    public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
