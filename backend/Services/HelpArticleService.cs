using FormBuilder.Backend.Models;
using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend.Services;

public class HelpArticleService
{
    private readonly FormBuilderDbContext _context;

    public HelpArticleService(FormBuilderDbContext context)
    {
        _context = context;
    }

    public void SaveArticle(HelpArticle article)
    {
        if (article.Id == 0)
        {
            _context.HelpArticles.Add(article);
        }
        else
        {
            _context.HelpArticles.Update(article);
        }

        _context.SaveChanges();
    }

    public List<HelpArticle> GetAllArticles()
    {
        return _context.HelpArticles.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public HelpArticle? GetArticleById(int id)
    {
        return _context.HelpArticles.FirstOrDefault(a => a.Id == id);
    }

    public void DeleteArticle(int id)
    {
        var article = _context.HelpArticles.FirstOrDefault(a => a.Id == id);
        if (article != null)
        {
            _context.HelpArticles.Remove(article);
            _context.SaveChanges();
        }
    }
}