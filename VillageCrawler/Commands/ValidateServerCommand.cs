using MediatR;
using Microsoft.Extensions.Options;
using VillageCrawler.Models.Options;

namespace VillageCrawler.Commands
{
    public record ValidateServerCommand(string Url) : IRequest<bool>;

    public class ValidateServerCommandHandler(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
        : IRequestHandler<ValidateServerCommand, bool>
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly AppSettings _appSettings = appSettings.Value;

        public async Task<bool> Handle(ValidateServerCommand request, CancellationToken cancellationToken)
        {
            var url = string.Format(_appSettings.UrlMapSql, request.Url);
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cancellationToken);
                if (!response.IsSuccessStatusCode) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}