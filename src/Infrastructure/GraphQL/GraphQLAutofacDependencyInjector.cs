namespace Sable
{
    using Autofac;
    using GraphQL.Conventions;

    public sealed class GraphQLAutofacDependencyInjector : IDependencyInjector
    {
        public GraphQLAutofacDependencyInjector(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        private readonly ILifetimeScope scope;

        public object Resolve(System.Reflection.TypeInfo typeInfo)
            => scope.TryResolve(typeInfo.AsType(), out var obj)
                ? obj
                : null;
    }
}
