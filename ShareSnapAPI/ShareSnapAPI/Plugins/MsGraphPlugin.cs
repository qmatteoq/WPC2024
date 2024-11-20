using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
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
        [Description("Post news to a SharePoint site using Microsoft Graph API")]
        public async Task PostNewsToSharePointAsync([Description("The title of the news")]string newsTitle, 
                                                    [Description("The content of the news")]string newsContent)
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

            try
            {
                var result = await graphClient.Sites["05fd9c1f-c6d9-4411-b8db-1887697747ad"]
                    .Pages.PostAsync(page);

                await graphClient.Sites["05fd9c1f-c6d9-4411-b8db-1887697747ad"]
                    .Pages
                    .WithUrl($"{graphClient.RequestAdapter.BaseUrl}/sites/05fd9c1f-c6d9-4411-b8db-1887697747ad/pages/{result.Id}/microsoft.graph.sitePage/publish")
                    .PostAsync(new BaseSitePage());
            }
            catch(Exception ex)
            {

            }

        }

        [KernelFunction("SendNewsByMail")]
        [Description("Send an article by mail to a given user")]
        public async Task SendNewsByMail([Description("The title of the news")] string newsTitle, 
                                        [Description("The content of the news")] string newsContent, 
                                        [Description("The mail subject")]string subject, 
                                        [Description("The recipient of the mail")]string recipient)
        {
            var users = await graphClient.Users.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"mail eq 'admin@M365CPI95634428.onmicrosoft.com'";
            });

            var user = users.Value.FirstOrDefault();
            SendMailPostRequestBody mailRequest = new SendMailPostRequestBody
            {
                Message = new Message
                {
                    Subject = subject,
                    ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = recipient
                        }
                    }
                },
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = $"{newsContent}"
                    }
                }
            };

            await graphClient.Users[user.Id].SendMail.PostAsync(mailRequest);
        }
    }
}
