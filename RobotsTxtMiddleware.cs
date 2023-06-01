namespace TonDomainInfoBot
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class RobotsTxtMiddleware
    {
        private const string Contents = "User-agent: * \r\nDisallow: / \r\n";

        private readonly RequestDelegate next;

        public RobotsTxtMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));

            var request = context.Request;

            if (request.Path != "/robots.txt")
            {
                return next?.Invoke(context) ?? Task.CompletedTask;
            }

            var response = context.Response;

            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/plain";
            return response.WriteAsync(Contents);
        }
    }
}
