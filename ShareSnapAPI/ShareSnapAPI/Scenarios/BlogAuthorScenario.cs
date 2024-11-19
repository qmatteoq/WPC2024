using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ShareSnapAPI.Plugins;
using ShareSnapAPI.Services;

namespace ShareSnapAPI.Scenarios
{
    public class BlogAuthorScenario : BaseScenario
    {
        public override void InitializeScenario(bool useAzureOpenAI)
        {
            #region Summarizer Agent
            string summarizerAgentName = "SummarizerAgent";
            string summarizeAgentYaml = File.ReadAllText("./Prompts/SummarizerAgent.yml");
            var summarizerAgentTemplate = KernelFunctionYaml.ToPromptTemplateConfig(summarizeAgentYaml);

            ChatCompletionAgent summarizerAgent = new ChatCompletionAgent(summarizerAgentTemplate)
            {
                Name = summarizerAgentName,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI)
            };
            #endregion

            #region Social Network Expert Agent
            string socialNetworkExpertAgentName = "SocialNetworkExpertAgent";
            string socialNetworkExpertYaml = File.ReadAllText("./Prompts/SocialNetworkExpertAgent.yml");
            var socialNetworkExpertAgentTemplate = KernelFunctionYaml.ToPromptTemplateConfig(socialNetworkExpertYaml);

            ChatCompletionAgent socialNetworkExpertAgent = new ChatCompletionAgent(socialNetworkExpertAgentTemplate)
            {
                Name = socialNetworkExpertAgentName,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI)
            };

            #endregion

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            #region Social Network Reviewer Agent

            string socialNetworkReviewerAgentName = "SocialNetworkReviewAgent";
            string socialNetworkReviewerAgentYaml = File.ReadAllText("./Prompts/SocialNetworkReviewerAgent.yml");
            var socialNetworkReviewerAgentTemplate = KernelFunctionYaml.ToPromptTemplateConfig(socialNetworkReviewerAgentYaml);

            ChatCompletionAgent socialNetworkReviewAgent = new ChatCompletionAgent
            {
                Name = socialNetworkReviewerAgentName,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI),
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };

            socialNetworkReviewAgent.Kernel.ImportPluginFromType<MsGraphPlugin>();

            #endregion

            #region Mail Share Agent

            string mailShareAgentName = "MailShareAgent";
            string mailShareAgentYaml = File.ReadAllText("./Prompts/MailShareAgent.yml");
            var mailShareAgentTemplate = KernelFunctionYaml.ToPromptTemplateConfig(mailShareAgentYaml);

            ChatCompletionAgent mailShareAgent = new ChatCompletionAgent
            {
                Name = mailShareAgentName,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI),
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };

            mailShareAgent.Kernel.ImportPluginFromType<MsGraphPlugin>();
            #endregion

            var terminateFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
               $$$"""
                Determine if the post for the social network has been published and the mail has been sent. If so, respond with a single word: yes.

                History:

                {{$history}}
                """,
                safeParameterNames: "history"
               );

            var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
                State only the name of the participant to take the next turn.

                Choose only from these participants:
                - {{{summarizerAgentName}}}
                - {{{socialNetworkExpertAgentName}}}
                - {{{socialNetworkReviewerAgentName}}}
                - {{{mailShareAgentName}}}

                Always follow these steps when selecting the next participant:
                1) After user input, it is {{{summarizerAgentName}}}'s turn to generate a summary of the given text.
                2) After {{{summarizerAgentName}}} replies, it's {{{socialNetworkExpertAgentName}}}'s turn to create the social network post.
                3) After {{{socialNetworkExpertAgentName}}} replies, it's {{{socialNetworkReviewerAgentName}}}'s turn to review the post and, if it's approved, to publish it on the selected social channel.
                4) After {{{socialNetworkReviewerAgentName}}} replies, it's {{{mailShareAgentName}}}'s turn to send the content via mail.
                
                History:
                {{$history}}
                """,
                safeParameterNames: "history"
                );

            chat = new(summarizerAgent, socialNetworkExpertAgent, socialNetworkReviewAgent, mailShareAgent)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, KernelCreator.CreateKernel(useAzureOpenAI))
                    {
                        Agents = [mailShareAgent],
                        ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                        HistoryVariableName = "history",
                        MaximumIterations = 10
                    },
                    SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, KernelCreator.CreateKernel(useAzureOpenAI))
                    {
                        InitialAgent = summarizerAgent,
                        AgentsVariableName = "agents",
                        HistoryVariableName = "history"
                    }
                }
            };

        }
    }
}
