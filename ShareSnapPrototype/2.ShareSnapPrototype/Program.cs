using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ShareSnapPrototype.Plugins;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string apiKey = configuration["AzureOpenAI:ApiKey"];
string deploymentName = configuration["AzureOpenAI:DeploymentName"];
string endpoint = configuration["AzureOpenAI:Endpoint"];

string openAIKey = configuration["OpenAI:ApiKey"];
string openAIModel = configuration["OpenAI:Model"];

var kernel = Kernel.CreateBuilder()
             .AddOpenAIChatCompletion(openAIModel, openAIKey)
             .Build();

string summarizeYaml = File.ReadAllText("./Prompts/Summarize.yml");
var promptFunction = kernel.CreateFunctionFromPromptYaml(summarizeYaml);
kernel.ImportPluginFromType<FilePlugin>();

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var result = await kernel.InvokePromptAsync(@"Summarize the following article, show it to the user then save it into a file: In the ever-evolving landscape of technology, Generative AI stands out as a transformative force with the potential to revolutionize enterprises across various industries. Unlike traditional AI, which relies on pre-set rules and data to make decisions or predictions, Generative AI can create new content and ideas, making it an invaluable tool for businesses looking to innovate and grow.\rEnhancing Creativity and Innovation\rOne of the most significant impacts of Generative AI on enterprises is its ability to enhance creativity and innovation. By leveraging advanced algorithms and vast amounts of data, Generative AI can produce unique designs, suggest new product ideas, and even generate compelling marketing content. This capability allows companies to explore new possibilities and stay ahead of the competition. For instance, in the fashion industry, designers can use Generative AI to create novel patterns and styles, while in marketing, it can help craft personalized advertisements that resonate with specific audiences.\rStreamlining Operations\rGenerative AI also plays a crucial role in streamlining business operations. It can automate complex tasks that typically require human expertise, such as data analysis, report generation, and even customer service. By doing so, it frees up valuable time and resources for employees to focus on more strategic initiatives. For example, financial institutions can use Generative AI to analyze market trends and generate insightful reports, while customer service departments can deploy AI-powered chatbots that handle routine inquiries efficiently and effectively.\rImproving Decision-Making\rThe ability of Generative AI to process and analyze vast amounts of data in real-time makes it an indispensable tool for improving decision-making. Enterprises can leverage AI-driven insights to identify patterns, forecast trends, and make informed decisions that drive business growth. In the healthcare sector, for instance, Generative AI can analyze patient data to predict disease outbreaks and suggest preventive measures, ultimately enhancing patient care and outcomes.\rPersonalizing Customer Experiences\rAnother transformative impact of Generative AI is its ability to personalize customer experiences. By analyzing customer behavior and preferences, AI can generate tailored recommendations and offers that cater to individual needs. This level of personalization not only increases customer satisfaction but also drives loyalty and retention. E-commerce platforms, for example, can use Generative AI to recommend products based on past purchases and browsing history, creating a more engaging and relevant shopping experience for users.\rDriving Efficiency and Cost Savings\rGenerative AI can significantly drive efficiency and cost savings for enterprises. By automating repetitive tasks and optimizing processes, businesses can reduce operational costs and improve overall efficiency. Manufacturing companies, for example, can use AI to optimize production lines and reduce waste, while logistics firms can leverage AI to plan efficient delivery routes, saving both time and money.\rConclusion\rIn conclusion, the impact of Generative AI on enterprises is profound and far-reaching. From enhancing creativity and innovation to streamlining operations, improving decision-making, personalizing customer experiences, and driving efficiency and cost savings, Generative AI offers a multitude of benefits that can help businesses thrive in today's competitive landscape. As AI technology continues to advance, its potential to transform enterprises will only grow, making it an essential tool for any forward-thinking organization.\r\r\r\r", new KernelArguments(openAIPromptExecutionSettings));


Console.WriteLine(result.GetValue<string>());
Console.ReadLine();