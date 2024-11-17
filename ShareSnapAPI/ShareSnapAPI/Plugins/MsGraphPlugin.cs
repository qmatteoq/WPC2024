using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ShareSnapAPI.Plugins
{
    public class MsGraphPlugin
    {
        private GraphServiceClient graphClient;
        public MsGraphPlugin()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // Set the base path for the configuration files
                .AddUserSecrets<Program>(); // Add the user secrets


            // Build the configuration
            var configuration = configurationBuilder.Build();

            var tenantId = configuration["MicrosoftGraph:TenantId"];
            var clientId = configuration["MicrosoftGraph:ClientId"];
            var clientSecret = configuration["MicrosoftGraph:ClientSecret"];

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            graphClient = new GraphServiceClient(clientSecretCredential);
        }

        [KernelFunction("PostNewsToSharePoint")]
        [Description("Posts news to a SharePoint site using Microsoft Graph API")]
        public async Task PostNewsToSharePointAsync(string newsTitle, string newsContent)
        {
            string sanitizedTitle = Regex.Replace(newsTitle, @"[^a-zA-Z0-9\s]", "");

            SitePage page = new SitePage
            {
                OdataType = "#microsoft.graph.sitePage",
                Title = newsTitle,
                Description = newsTitle,
                ShowComments = true,
                ShowRecommendedPages = false,
                Name = $"{sanitizedTitle}.aspx",
                PageLayout = PageLayoutType.Article,
                PromotionKind = PagePromotionType.NewsPost,
                CanvasLayout = new CanvasLayout
                {
                    HorizontalSections = new List<HorizontalSection>
                    {
                        new HorizontalSection
                        {
                            Layout = HorizontalSectionLayoutType.FullWidth,
                            Id = "1",
                            Emphasis = SectionEmphasisType.None,
                            Columns = new List<HorizontalSectionColumn>
                            {
                                new HorizontalSectionColumn
                                {
                                    Id = "1",
                                    Width = 0,
                                    Webparts = new List<WebPart>
                                    {
                                        new TextWebPart
                                        {
                                            InnerHtml = $"<div><h1>{newsTitle}</h1></div> <p>{newsContent}</p>"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var result = await graphClient.Sites["05fd9c1f-c6d9-4411-b8db-1887697747ad"]
                .Pages.PostAsync(page);

            await graphClient.Sites["05fd9c1f-c6d9-4411-b8db-1887697747ad"]
                .Pages
                .WithUrl($"{graphClient.RequestAdapter.BaseUrl}/sites/05fd9c1f-c6d9-4411-b8db-1887697747ad/pages/{result.Id}/microsoft.graph.sitePage/publish")
                .PostAsync(new BaseSitePage());

        }



        [KernelFunction("AddAppointment")]
        [Description("Add an appointment to the user's calendar give the date and the subject")]
        public async Task AddAppointmentToCalendar(DateTime date, string subject, string email)
        {
            var users = await graphClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"mail eq '{email}'";
            });

            var user = users.Value.FirstOrDefault();

            await graphClient.Users[user.Id].Calendar.Events.PostAsync(new Event
            {
                Subject = subject,
                Start = new DateTimeTimeZone
                {
                    DateTime = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = date.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                }
            });
        }
    }
}
