using System.Net;
using System.Text;

namespace GameRecommendation.Web.Tests
{
    /// <summary>
    /// A minimal <see cref="HttpMessageHandler"/> test double that returns a canned response and
    /// records every request sent through it, so service tests in this project don't have to
    /// repeat Moq.Protected boilerplate for each HTTP call.
    /// </summary>
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public List<HttpRequestMessage> Requests { get; } = new();

        public HttpRequestMessage? LastRequest => Requests.Count > 0 ? Requests[^1] : null;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string? content = null)
            : this(_ => new HttpResponseMessage(statusCode)
            {
                // Explicit UTF-8 + application/json, matching what the real API actually sends --
                // the bare StringContent(string) constructor defaults to text/plain.
                Content = new StringContent(content ?? string.Empty, Encoding.UTF8, "application/json")
            })
        {
        }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            this.responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
