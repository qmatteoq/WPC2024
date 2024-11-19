using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ShareSnapPrototype.Plugins
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

        [KernelFunction("SendNewsByMail")]
        [Description("Send an article by mail to a given user")]
        public async Task SendNewsByMail([Description("The title of the news")] string newsTitle,
                                        [Description("The content of the news")] string newsContent,
                                        [Description("The mail subject")] string subject,
                                        [Description("The recipient of the mail")] string recipient)
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
