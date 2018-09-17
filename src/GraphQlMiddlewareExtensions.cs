namespace Sable
{
    using System.Reflection;
    using Autofac;
    using GraphQL.Conventions;
    using GraphQL.Conventions.Web;
    using GraphQL.Validation;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public static class GraphQlMiddlewareExtensions
    {
        public static IApplicationBuilder UseGraphQL(
            this IApplicationBuilder builder, PathString pathPrefix)
        {
            return builder.UseMiddleware<GraphQLMiddleware>(pathPrefix);
        }

        public static IApplicationBuilder UseGraphQL(this IApplicationBuilder builder)
            => builder.UseGraphQL("/graphql");

        public static ContainerBuilder AddGrapqhQL<TQuery, TMutation>(this ContainerBuilder builder)
            where TQuery: class
            where TMutation: class
        {
            builder.RegisterType<GraphQLAutofacDependencyInjector>()
                .As<IDependencyInjector>();

            builder.Register(ctx =>
                {
                    var injector = ctx.Resolve<IDependencyInjector>();
                    var handler = RequestHandler
                        .New()
                        .WithDependencyInjector(injector)
                        .WithQueryAndMutation<TQuery, TMutation>()
                        .WithProfiling()
                        .Generate();

                    return handler;
                })
                .As<IRequestHandler>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TQuery>()
                .AsSelf()
                .InstancePerLifetimeScope();

            builder.RegisterType<TMutation>()
                .AsSelf()
                .InstancePerLifetimeScope();

            return builder;
        }
    }
}
