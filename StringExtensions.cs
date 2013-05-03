
namespace OrchardHUN.ExternalPages
{
    public static class StringExtensions
    {
        public static bool IsMarkdownFilePath(this string path)
        {
            return path.EndsWith(".md");
        }
    }
}