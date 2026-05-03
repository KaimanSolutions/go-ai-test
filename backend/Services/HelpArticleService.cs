using FormBuilder.Backend.Models;
using System.Collections.Generic;

namespace FormBuilder.Backend.Services;

public class HelpArticleService
{
    private readonly List<HelpArticle> _articles = new(); // Replace with actual database logic

    public void SaveArticle(HelpArticle article)
    {
        article.Id = _articles.Count + 1;
        _articles.Add(article);
    }

    public List<HelpArticle> GetAllArticles()
    {
        return _articles;
    }
}