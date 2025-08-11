namespace PdfTitleRenamer.Models
{
    public class PdfMetadata
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Keywords { get; set; } = "";
        public string Creator { get; set; } = "";
        public string Producer { get; set; } = "";

        public bool HasAnyData => !string.IsNullOrWhiteSpace(Title) || 
                                 !string.IsNullOrWhiteSpace(Author) || 
                                 !string.IsNullOrWhiteSpace(Subject) || 
                                 !string.IsNullOrWhiteSpace(Keywords);

        public override string ToString()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(Title)) parts.Add($"Title: {Title}");
            if (!string.IsNullOrWhiteSpace(Author)) parts.Add($"Author: {Author}");
            if (!string.IsNullOrWhiteSpace(Subject)) parts.Add($"Subject: {Subject}");
            if (!string.IsNullOrWhiteSpace(Keywords)) parts.Add($"Keywords: {Keywords}");
            
            return string.Join(", ", parts);
        }
    }
}
