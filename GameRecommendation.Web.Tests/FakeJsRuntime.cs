using Microsoft.JSInterop;

namespace GameRecommendation.Web.Tests
{
    /// <summary>
    /// A minimal in-memory <see cref="IJSRuntime"/> double simulating just enough of
    /// <c>localStorage</c> for testing <see cref="GameRecommendation.Web.Auth.JwtAuthenticationStateProvider"/>.
    /// </summary>
    /// <remarks>
    /// Moq can't cleanly mock this interface: <c>InvokeVoidAsync</c> is an extension method that
    /// calls <c>InvokeAsync&lt;TValue&gt;</c> with an internal marker type (<c>IJSVoidResult</c>)
    /// that test code can't name. A small hand-written fake sidesteps that entirely, in the same
    /// spirit as <c>FakeLogger&lt;T&gt;</c>.
    /// </remarks>
    public class FakeJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string?> store = new();

        /// <summary>The (identifier, args) pair for every invocation, in call order.</summary>
        public List<(string Identifier, object?[]? Args)> Invocations { get; } = new();

        /// <summary>When set, every invocation throws this exception instead of executing.</summary>
        public Exception? ThrowOnInvoke { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args));

            if (ThrowOnInvoke != null)
                throw ThrowOnInvoke;

            switch (identifier)
            {
                case "localStorage.getItem":
                    var getKey = (string)args![0]!;
                    var value = store.TryGetValue(getKey, out var v) ? v : null;
                    return new ValueTask<TValue>((TValue)(object)value!);

                case "localStorage.setItem":
                    store[(string)args![0]!] = (string?)args[1];
                    return default;

                case "localStorage.removeItem":
                    store.Remove((string)args![0]!);
                    return default;

                default:
                    throw new NotSupportedException($"FakeJsRuntime received an unexpected call: '{identifier}'");
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        /// <summary>Seeds localStorage with a token, as if a previous session had already logged in.</summary>
        public void SeedToken(string token) => store["authToken"] = token;
    }
}
