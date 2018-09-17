namespace Sable
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using GraphQL.Conventions;
    using GraphQL.Conventions.Web;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class GraphQLMiddleware
    {
        public GraphQLMiddleware(PathString pathPrefix, RequestDelegate next)
        {
            this.pathPrefix = pathPrefix;
            this.next = next;
        }

        private readonly PathString pathPrefix;
        private readonly RequestDelegate next;

        public async Task InvokeAsync(HttpContext context, ILifetimeScope parentScope)
        {
            if (!context.Request.Path.StartsWithSegments(pathPrefix))
            {
                await next(context);
                return;
            }

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.StatusCode = 200;
                return;
            }

            if (!HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = 400;
                return;
            }

            Response result;
            using (var scope = parentScope.BeginLifetimeScope())
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                var body = streamReader.ReadToEnd();

                var requestHandler = scope.Resolve<IRequestHandler>();
                result = await requestHandler
                    .ProcessRequest(Request.New(body), null);
            }

            context.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            context.Response.StatusCode = result.Errors?.Count > 0 ? 400 : 200;
            await context.Response.WriteAsync(result.Body);
        }
    }
}
