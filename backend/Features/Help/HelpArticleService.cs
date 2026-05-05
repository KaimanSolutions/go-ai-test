using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

public sealed class HelpArticleService
{
    private readonly FormBuilderDbContext _context;

    public HelpArticleService(FormBuilderDbContext context) => _context = context;

    public List<HelpArticle> GetAllArticles() =>
        _context.HelpArticles.OrderByDescending(a => a.CreatedAt).ToList();

    public List<HelpArticle> GetArticlesForPortal(string? role) => role switch
    {
        UserRoles.Admin  => GetAllArticles(),
        UserRoles.Broker => _context.HelpArticles
            .Where(a => a.ShowInBrokerPortal)
            .OrderByDescending(a => a.CreatedAt).ToList(),
        _ => _context.HelpArticles
            .Where(a => a.ShowInCustomerPortal)
            .OrderByDescending(a => a.CreatedAt).ToList()
    };

    public HelpArticle? GetArticleById(int id) =>
        _context.HelpArticles.FirstOrDefault(a => a.Id == id);

    public void SaveArticle(HelpArticle article)
    {
        if (article.Id == 0) _context.HelpArticles.Add(article);
        else                  _context.HelpArticles.Update(article);
        _context.SaveChanges();
    }

    public void DeleteArticle(int id) =>
        _context.HelpArticles.Where(a => a.Id == id).ExecuteDelete();
}
