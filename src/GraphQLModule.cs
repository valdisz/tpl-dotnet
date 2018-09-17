namespace Sable
{
    using Autofac;

    public sealed class GraphQLModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddGrapqhQL<Query, Mutation>();
        }
    }
}
