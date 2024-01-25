using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurants.RestApi.Tests
{
    /// <summary>
    /// 이 클래스는 멀티테넌트 시스템으로 확장되기 이전의 API와 상호작용했던 
    /// 일반적인 HTTP 클라이언트가 예상하는 상호작용들을 캡슐화 한 것입니다. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이 클래스는 API의 일반적인 레거시 클라이언트를 모사하기 위해서 
    /// 만들어졌으므로, 시스템에 문제가 발생할 만한 변경사항들이 들어왔는지 
    /// 확인하는데 유용합니다. 
    /// 따라서, 이 클래스의 코드는 거의(혹은 전혀) 편집될 필요가 없습니다. 
    /// </para>
    /// <para>
    /// 이 클래스에 있는 코드가 편집되지 않길 바라는 것은 비현실적일수도 있으나,
    /// 편집을 하더라도 고민후에 세심한 주의를 기울여야만 합니다. 
    /// 변수 이름의 오타를 수정하는 수준의 간단한 편집이어야 하며, 
    /// 동작을 변경해서는 안됩니다. 
    /// </para>
    /// <para>
    /// 이 규칙에 대한 예외는 
    /// <see cref="ConfigureWebHost(IWebHostBuilder)" />를 
    /// 오버라이드 시키고, 미래에 있을 기타 상상할 수 있는 것들을 
    /// 역시 오버라이드 시켜야 합니다.
    /// 이는 외부로 보이는 동작으로 나타나지는 않을 가능성이 높지만,
    /// 반대로 동작의 일관성을 유지하는데 사용될 수 있습니다. 
    /// </para>
    /// <para>
    /// 만일 원격 측정, 계약 지원 등을 통해서 레거시 클라인언트가 
    /// 더 이상 존재하지 않거나 지원하지 않아도 된다는 것이 확인되면
    /// 위의 제한 사항을 지키지 않아도 됩니다.
    /// </para>
    /// </remarks>
    public sealed class LegacyApi : WebApplicationFactory<Startup>
    {
        private bool authorizeClient;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReservationsRepository>();
                services.AddSingleton<IReservationsRepository>(
                    new FakeDatabase());
            });
        }

        internal void AuthorizeClient()
        {
            authorizeClient = true;
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (!authorizeClient)
                return;

            var token = GenerateJwtToken();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private static string GenerateJwtToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(
                "This is not the secret used in production.");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject =
                    new ClaimsIdentity(new[]
                    {
                        new Claim("role", "MaitreD"),
                        new Claim("restaurant", $"{Grandfather.Id}")
                    }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<HttpResponseMessage> PostReservation(
            object reservation)
        {
            string json = JsonSerializer.Serialize(reservation);
            using var content = new StringContent(json);
            content.Headers.ContentType.MediaType = "application/json";

            var address = await FindAddress("urn:reservations");
            return await CreateClient().PostAsync(address, content);
        }

        public async Task<HttpResponseMessage> PutReservation(
            Uri address,
            object reservation)
        {
            string json = JsonSerializer.Serialize(reservation);
            using var content = new StringContent(json);
            content.Headers.ContentType.MediaType = "application/json";
            return await CreateClient().PutAsync(address, content);
        }

        public async Task<HttpResponseMessage> GetCurrentYear()
        {
            var yearAddress = await FindAddress("urn:year");
            return await CreateClient().GetAsync(yearAddress);
        }

        public async Task<HttpResponseMessage> GetPreviousYear()
        {
            var currentResp = await GetCurrentYear();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("previous");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetNextYear()
        {
            var currentResp = await GetCurrentYear();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("next");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetYear(int year)
        {
            var resp = await GetCurrentYear();
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();
            if (dto.Year == year)
                return resp;

            var rel = dto.Year < year ? "next" : "previous";
            
            var client = CreateClient();
            do
            {
                var address = dto.Links.FindAddress(rel);
                resp = await client.GetAsync(address);
                resp.EnsureSuccessStatusCode();
                dto = await resp.ParseJsonContent<CalendarDto>();
            } while (dto.Year != year);
            
            return resp;
        }

        public async Task<HttpResponseMessage> GetCurrentMonth()
        {
            var monthAddress = await FindAddress("urn:month");
            return await CreateClient().GetAsync(monthAddress);
        }

        public async Task<HttpResponseMessage> GetPreviousMonth()
        {
            var currentResp = await GetCurrentMonth();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("previous");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetNextMonth()
        {
            var currentResp = await GetCurrentMonth();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("next");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetMonth(int year, int month)
        {
            var resp = await GetYear(year);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();

            var target = new DateTime(year, month, 1).ToIso8601DateString();
            var monthCalendar = dto.Days.Single(d => d.Date == target);
            var address = monthCalendar.Links.FindAddress("urn:month");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetCurrentDay()
        {
            var dayAddress = await FindAddress("urn:day");
            return await CreateClient().GetAsync(dayAddress);
        }

        public async Task<HttpResponseMessage> GetPreviousDay()
        {
            var currentResp = await GetCurrentDay();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("previous");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetNextDay()
        {
            var currentResp = await GetCurrentDay();
            currentResp.EnsureSuccessStatusCode();
            var dto = await currentResp.ParseJsonContent<CalendarDto>();
            var address = dto.Links.FindAddress("next");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetDay(
            int year,
            int month,
            int day)
        {
            var resp = await GetYear(year);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();

            var target = new DateTime(year, month, day).ToIso8601DateString();
            var dayCalendar = dto.Days.Single(d => d.Date == target);
            var address = dayCalendar.Links.FindAddress("urn:day");
            return await CreateClient().GetAsync(address);
        }

        public async Task<HttpResponseMessage> GetSchedule(
            int year,
            int month,
            int day)
        {
            var resp = await GetYear(year);
            resp.EnsureSuccessStatusCode();
            var dto = await resp.ParseJsonContent<CalendarDto>();

            var target = new DateTime(year, month, day).ToIso8601DateString();
            var dayCalendar = dto.Days.Single(d => d.Date == target);
            var address = dayCalendar.Links.FindAddress("urn:schedule");
            return await CreateClient().GetAsync(address);
        }

        private async Task<Uri> FindAddress(string rel)
        {
            var homeResponse =
                await CreateClient().GetAsync(new Uri("", UriKind.Relative));
            homeResponse.EnsureSuccessStatusCode();
            var homeRepresentation =
                await homeResponse.ParseJsonContent<HomeDto>();

            return homeRepresentation.Links.FindAddress(rel);
        }
    }
}
