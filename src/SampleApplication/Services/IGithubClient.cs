using System.Threading.Tasks;

namespace SampleApplication.Services
{
    public interface IGithubClient
  {
    Task<GithubUser> GetUserAsync(string userName);
  }
}