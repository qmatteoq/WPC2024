using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ShareSnapPrototype.Plugins
{
    public class FilePlugin
    {
        [KernelFunction("SaveToFile")]
        [Description("A function to save a text into a file")]
        public async Task SaveToFile([Description("The name of the file")]string fileName, 
                                     [Description("The text to save")]string content)
        {
            string path = $"./Documents/{fileName}.txt";
            await File.WriteAllTextAsync(path, content);
        }
    }
}
