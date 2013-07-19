
namespace OrchardHUN.ExternalPages
{
    public static class StringExtensions
    {
        public static bool IsMarkdownFilePath(this string path)
        {
            return path.EndsWith(".md");
        }

        public static bool IsIndexFilePath(this string path)
        {
            return path.EndsWith("Index.md");
        }
    }
}