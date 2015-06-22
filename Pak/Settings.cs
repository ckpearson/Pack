using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;

namespace Pack
{
    public static class Settings
    {
        public static async Task<LoginInfo> GetProxyCredentials()
        {
            return await BlobCache.Secure.GetLoginAsync();
        }
    }
}