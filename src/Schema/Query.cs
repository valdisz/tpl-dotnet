namespace Sable
{
    using System.Threading.Tasks;
    using GraphQL.Conventions;
    using Microsoft.AspNetCore.Identity;

    public class Query
    {
        // public Query(
        //     [Inject] UserManager<IdentityUser> userManager

        // ) {
        //     this.userManager = userManager;
        // }

        public Task<bool> Test() {
            return Task.FromResult(true);
        }
    }
}
