﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.IO;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Office365CalendarDemo.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace Office365CalendarDemo.Models
{
    public class MyEventsRepository
    {
        public async Task<List<MyEvent>> GetEvents()
        {

            var eventsResults = new List<MyEvent>();
            var accessToken = await GetGraphAccessTokenAsync();
            var restURL = string.Format("{0}/me/calendar/events?$top=10&$filter=End/DateTime ge '{1}'", SettingsHelper.GraphResourceUrl, DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var accept = "application/json";

                    client.DefaultRequestHeaders.Add("Accept", accept);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.GetAsync(restURL))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonresult = JObject.Parse(await response.Content.ReadAsStringAsync());

                            foreach (var item in jsonresult["value"])
                            {
                                eventsResults.Add(new MyEvent
                                {
                                    Start = !string.IsNullOrEmpty(item["start"]["dateTime"].ToString()) ? DateTime.Parse(item["start"]["dateTime"].ToString()) : new DateTime(),
                                    End = !string.IsNullOrEmpty(item["end"]["dateTime"].ToString()) ? DateTime.Parse(item["end"]["dateTime"].ToString()) : new DateTime(),
                                    Id = !string.IsNullOrEmpty(item["id"].ToString()) ? item["id"].ToString() : string.Empty,
                                    Subject = !string.IsNullOrEmpty(item["subject"].ToString()) ? item["subject"].ToString() : string.Empty,
                                    Body = !string.IsNullOrEmpty(item["body"].ToString()) ? item["body"]["content"].ToString() : string.Empty,
                                    Location = !string.IsNullOrEmpty(item["location"].ToString()) ? item["location"]["displayName"].ToString() : string.Empty,
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception el)
            {
                el.ToString();
            }

            return eventsResults.OrderBy(e => e.Start).ToList();

        }

        public async Task<MyEvent> GetEvent(string id)
        {
            var accessToken = await GetGraphAccessTokenAsync();
            var restURL = string.Format("{0}me/events/{1}", SettingsHelper.GraphResourceUrl, id);
            var ev = new MyEvent();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var accept = "application/json";

                    client.DefaultRequestHeaders.Add("Accept", accept);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.GetAsync(restURL))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var item = JObject.Parse(await response.Content.ReadAsStringAsync());

                            if (item != null)
                            {
                                ev.Start = !string.IsNullOrEmpty(item["start"]["dateTime"].ToString()) ? DateTime.Parse(item["start"]["dateTime"].ToString()) : new DateTime();
                                ev.End = !string.IsNullOrEmpty(item["end"]["dateTime"].ToString()) ? DateTime.Parse(item["end"]["dateTime"].ToString()) : new DateTime();
                                ev.Id = !string.IsNullOrEmpty(item["id"].ToString()) ? item["id"].ToString() : string.Empty;
                                ev.Subject = !string.IsNullOrEmpty(item["subject"].ToString()) ? item["subject"].ToString() : string.Empty;
                                ev.Body = !string.IsNullOrEmpty(item["body"].ToString()) ? item["body"]["content"].ToString() : string.Empty;
                                ev.Location = !string.IsNullOrEmpty(item["location"].ToString()) ? item["location"]["displayName"].ToString() : string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception el)
            {
                el.ToString();
            }

            return ev;
        }

        public async Task DeleteEvent(string id)
        {
            var accessToken = await GetGraphAccessTokenAsync();
            var restURL = string.Format("{0}me/events('{1}')", SettingsHelper.GraphResourceUrl, id);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var accept = "application/json";

                    client.DefaultRequestHeaders.Add("Accept", accept);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    using (var response = await client.DeleteAsync(restURL))
                    {
                        if (response.IsSuccessStatusCode)
                            return;
                        else
                            throw new Exception("delete event error: " + response.StatusCode);
                    }
                }
            }
            catch (Exception el)
            {
                el.ToString();
            }
        }

        public async Task AddEvent(MyEvent myEvent)
        {
            var accessToken = await GetGraphAccessTokenAsync();
            var restURL = string.Format("{0}me/events", SettingsHelper.GraphResourceUrl);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var accept = "application/json";

                    client.DefaultRequestHeaders.Add("Accept", accept);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var ev = new Event
                    {
                        Subject = myEvent.Subject,
                        Start = new Start { dateTime = myEvent.Start.ToString() },
                        End = new End { dateTime = myEvent.End.ToString() },
                        Location = new Location { displayName = myEvent.Location }
                    };
                    string postBody = JsonConvert.SerializeObject(ev);

                    using (var response = await client.PostAsync(restURL, new StringContent(postBody, Encoding.UTF8, "application/json")))
                    {
                        if (response.IsSuccessStatusCode)
                            return;
                        else
                            throw new Exception("add event error: " + response.StatusCode);
                    }

                }
            }
            catch (Exception el)
            {
                el.ToString();
            }
        }

        public async Task<string> GetGraphAccessTokenAsync()
        {
            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst(SettingsHelper.ClaimTypeObjectIdentifier).Value;

            var clientCredential = new ClientCredential(SettingsHelper.ClientId, SettingsHelper.ClientSecret);
            var userIdentifier = new UserIdentifier(userObjectId, UserIdentifierType.UniqueId);

            // create auth context
            AuthenticationContext authContext = new AuthenticationContext(SettingsHelper.AzureAdAuthority, new ADALTokenCache(signInUserId));
            var result = await authContext.AcquireTokenSilentAsync(SettingsHelper.AzureAdGraphResourceURL, clientCredential, userIdentifier);

            return result.AccessToken;
        }

    }
}