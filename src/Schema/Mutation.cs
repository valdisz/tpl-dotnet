namespace Sable
{
    using System.Threading.Tasks;
    using GraphQL.Conventions;

    public class Mutation  {
        // public Mutation(
        //     [Inject] UserManager<IdentityUser> userManager,
        //     [Inject] IEmailSender emailSender
        // ) {
        //     this.userManager = userManager;
        //     this.emailSender = emailSender;
        // }

        public Task<bool> Signup(
            IResolutionContext context//,
            // NonNull<SignupInpuV1> input
        ) {
            return Task.FromResult(true);
        }
    }
}
