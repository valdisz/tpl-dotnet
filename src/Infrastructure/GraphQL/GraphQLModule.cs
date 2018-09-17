namespace Sable
{
    using Autofac;

    public sealed class GraphQLModule<TQuery, TMutation> : Autofac.Module
            where TQuery: class
            where TMutation: class
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddGrapqhQL<TQuery, TMutation>();
        }
    }
}
