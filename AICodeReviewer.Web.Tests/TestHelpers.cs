
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AICodeReviewer.Web.Tests
{
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new Dictionary<string, byte[]>();

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _data.Keys;

        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;

        bool ISession.TryGetValue(string key, out byte[] value)
        {
            return _data.TryGetValue(key, out value!);
        }

        public bool TryGetValue(string key, out byte[]? value)
        {
            byte[] result;
            var found = ((ISession)this).TryGetValue(key, out result!);
            value = result;
            return found;
        }
    }

    public static class SessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string? GetString(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        public static byte[]? Get(this ISession session, string key)
        {
            byte[]? value;
            session.TryGetValue(key, out value);
            return value;
        }
    }
}
