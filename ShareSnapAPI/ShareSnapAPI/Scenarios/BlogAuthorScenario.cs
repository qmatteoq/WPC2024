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
            string summarizerAgentName = "SummarizerAgent";
            string summarizerAgentInstructions = @"You are an expert summarization agent tasked with condensing long texts into concise and engaging summaries. Your objective is to distill the provided article into a clear, one-paragraph summary, ideally no more than 500 words. Focus on capturing the main points, key insights, or notable takeaways in a way that is both accessible and appealing to a general audience. Use simple, straightforward language to ensure clarity and avoid overly technical terms. The user will share with you a text to summarize and a target platform they want to share the post to. Please focus only on the summarization task, you MUST NOT generate the social network post. The post will be generate by the Social Network Expetr agent, which will use your summary as a starting point to create the post.";

            string socialNetworkExpertAgentName = "SocialNetworkExpertAgent";
            string socialNetworkExpertAgentInstructions = @"You are a social media expert with a keen eye for engaging content. Your role is to review the summary provided by the summarizer agent and write a post targeted for the social network given by the user. Consider the tone, style, and relevance of the summary, ensuring it is engaging, shareable, and likely to resonate with a broad audience. Make sure also that the post uses a tone which is fitting for the target social network. For example, use a more professional tone when the post will be published on LinkedIn or SharePoint; use, instead, a more casual tone when the post will be published on X or Facebook.";

            string socialNetworkShareAgentName = "SocialNetworkShareAgent";
            string socialNetworkShareAgentInstructions = @"You are a social media manager responsible for sharing engaging content on various channels. Your task is to review the post created by the social network expert agent and post it on the social channel given by the user. You MUST NOT generate the post, just review the one you are provided to and post it on the appropriate channel. Make sure to follow the guidelines of the social network and ensure that the post is engaging, shareable, and likely to resonate with the target audience. You have access to a series of tools to post the content on the following networks:

                - SharePoint
                - LinkedIn
                - Facebook";

            ChatCompletionAgent summarizerAgent = new ChatCompletionAgent
            {
                Name = summarizerAgentName,
                Instructions = summarizerAgentInstructions,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI)
            };

            ChatCompletionAgent socialNetworkExpertAgent = new ChatCompletionAgent
            {
                Name = socialNetworkExpertAgentName,
                Instructions = socialNetworkExpertAgentInstructions,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI)
            };

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            ChatCompletionAgent socialNetworkShareAgent = new ChatCompletionAgent
            {
                Name = socialNetworkShareAgentName,
                Instructions = socialNetworkShareAgentInstructions,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI),
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };

            socialNetworkShareAgent.Kernel.ImportPluginFromType<MsGraphPlugin>();

            KernelFunction terminateFunction = KernelFunctionFactory.CreateFromPrompt(
               $$$"""
                Determine if the post for the social network has been posted. If so, respond with a single word: yes.

                History:

                {{$history}}
                """
               );

            KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
                State only the name of the participant to take the next turn.

                Choose only from these participants:
                - {{{summarizerAgentName}}}
                - {{{socialNetworkExpertAgentName}}}
                - {{{socialNetworkShareAgentName}}}

                Always follow these steps when selecting the next participant:
                1) After user input, it is {{{summarizerAgentName}}}'s turn to generate a summary of the given text.
                2) After {{{summarizerAgentName}}} replies, it's {{{socialNetworkExpertAgentName}}}'s turn to create the social network post.
                3) After {{{socialNetworkExpertAgentName}}} replies, it's {{{socialNetworkShareAgentName}}}'s turn to post the content on the selected social network.
                
                History:
                {{$history}}
                """
                );

            chat = new(summarizerAgent, socialNetworkExpertAgent, socialNetworkShareAgent)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, KernelCreator.CreateKernel(useAzureOpenAI))
                    {
                        Agents = [socialNetworkShareAgent],
                        ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                        HistoryVariableName = "history",
                        MaximumIterations = 10
                    },
                    SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, KernelCreator.CreateKernel(useAzureOpenAI))
                    {
                        AgentsVariableName = "agents",
                        HistoryVariableName = "history"
                    }
                }
            };

        }
    }
}
