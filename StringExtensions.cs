
namespace OrchardHUN.ExternalPages
{
    public static class StringExtensions
    {
        public static bool IsMarkdownFilePath(this string path)
        {
            return path.EndsWith(".md", System.StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsIndexFilePath(this string path)
        {
            return path.EndsWith("Index.md", System.StringComparison.InvariantCultureIgnoreCase);
        }
    }
}