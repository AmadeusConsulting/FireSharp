using System.Net.Http;
using System.Threading.Tasks;

namespace FireSharp.Security
{
    public interface IRequestAuthenticator
    {
        Task AddAuthentication(HttpRequestMessage request);
    }
}