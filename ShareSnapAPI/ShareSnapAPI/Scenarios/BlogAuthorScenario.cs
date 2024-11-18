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

            string socialNetworkReviewAgentName = "SocialNetworkReviewAgent";
            string socialNetworkReviewAgentInstructions = @"You are a social media manager responsible for reviewing social network posts and share them on various channels. Your task is to review the post created by the social network expert agent. You must ensure that the post follows the guidelines of the target network and it's engaging, shareable, and likely to resonate with the target audience. You MUST NOT generate the post, just review the one you are provided. If you approve the post, then you can go on and post it. You have access to a tool that you can use to publish a post on the following channels:

                - SharePoint
                - LinkedIn
                - Facebook

               If the post isn't approved, instead, abort the operation";

            string mailShareAgentName = "MailShareAgent";
            string mailShareAgentInstructions = @"You are a mail manager responsible for sharing engaging content via mail. Your task is to review the post created by the social network expert agent and send it via mail to the given user. The mail address of the recipient will be shared by the user. You MUST use it, you MUST NOT generate another mail address.
            You MUST NOT generate the post, just review the one you are provided to and share it via mail.";

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

            ChatCompletionAgent socialNetworkReviewAgent = new ChatCompletionAgent
            {
                Name = socialNetworkReviewAgentName,
                Instructions = socialNetworkReviewAgentInstructions,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI),
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };

            socialNetworkReviewAgent.Kernel.ImportPluginFromType<MsGraphPlugin>();

            ChatCompletionAgent mailShareAgent = new ChatCompletionAgent
            {
                Name = mailShareAgentName,
                Instructions = mailShareAgentInstructions,
                Kernel = KernelCreator.CreateKernel(useAzureOpenAI),
                Arguments = new KernelArguments(openAIPromptExecutionSettings)
            };

            mailShareAgent.Kernel.ImportPluginFromType<MsGraphPlugin>();

            KernelFunction terminateFunction = KernelFunctionFactory.CreateFromPrompt(
               $$$"""
                Determine if the post for the social network has been published and the mail has been sent. If so, respond with a single word: yes.

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
                - {{{socialNetworkReviewAgentName}}}
                - {{{mailShareAgentName}}}

                Always follow these steps when selecting the next participant:
                1) After user input, it is {{{summarizerAgentName}}}'s turn to generate a summary of the given text.
                2) After {{{summarizerAgentName}}} replies, it's {{{socialNetworkExpertAgentName}}}'s turn to create the social network post.
                3) After {{{socialNetworkExpertAgentName}}} replies, it's {{{socialNetworkReviewAgentName}}}'s turn to review the post and, if it's approved, to publish it on the selected social channel.
                4) After {{{socialNetworkReviewAgentName}}} replies, it's {{{mailShareAgentName}}}'s turn to send the content via mail.
                
                History:
                {{$history}}
                """
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
                        AgentsVariableName = "agents",
                        HistoryVariableName = "history"
                    }
                }
            };

        }
    }
}
