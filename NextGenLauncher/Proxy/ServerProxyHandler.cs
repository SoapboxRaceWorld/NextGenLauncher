using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.Responses;

namespace NextGenLauncher.Proxy
{
    public class ServerProxyHandler : IApplicationStartup
    {
        public ServerProxyHandler()
        {
            
        }

        public void Initialize(IPipelines pipelines)
        {
            pipelines.BeforeRequest += ProxyRequest;
        }

        private async Task<Response> ProxyRequest(NancyContext ctx, CancellationToken ct)
        {
            Debug.WriteLine("{0} - {1}", ctx.Request.Method, ctx.Request.Path);

            foreach (var requestHeader in ctx.Request.Headers)
            {
                Debug.WriteLine("\t{0}: {1}", requestHeader.Key, string.Join(" ; ", requestHeader.Value));
            }

            // Build new request
            var url = new Flurl.Url(ServerProxy.Instance.GetCurrentServer().ServerAddress)
                .AppendPathSegment(ctx.Request.Path.Replace("/nfsw/Engine.svc", ""));
            foreach (var key in ctx.Request.Query)
            {
                url = url.SetQueryParam(key, ctx.Request.Query[key], NullValueHandling.Ignore);
            }

            IFlurlRequest request = url.WithTimeout(TimeSpan.FromSeconds(30));

            foreach (var requestHeader in ctx.Request.Headers)
            {
                request = request.WithHeader(requestHeader.Key, requestHeader.Value.First());
            }

            HttpResponseMessage responseMessage;

            switch (ctx.Request.Method)
            {
                case "GET":
                    responseMessage = await request.GetAsync(ct);
                    break;
                case "POST":
                    responseMessage =
                        await request.PostAsync(new CapturedStringContent(ctx.Request.Body.AsString(Encoding.UTF8)), ct);
                    break;
                case "PUT":
                    responseMessage =
                        await request.PutAsync(new CapturedStringContent(ctx.Request.Body.AsString(Encoding.UTF8)), ct);
                    break;
                case "DELETE":
                    responseMessage =
                        await request.DeleteAsync(ct);
                    break;
                default:
                    throw new ServerProxyException("Cannot handle request method: " + ctx.Request.Method);
            }

            return new TextResponse(await responseMessage.Content.ReadAsStringAsync(),
                responseMessage.Content.Headers.ContentType?.MediaType ?? "application/xml;charset=UTF-8")
            {
                StatusCode = (HttpStatusCode)(int)responseMessage.StatusCode
            };
        }
    }
}