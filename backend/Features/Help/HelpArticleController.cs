using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
public sealed class HelpArticleController : ControllerBase
{
    private readonly HelpArticleService _helpArticleService;

    public HelpArticleController(HelpArticleService helpArticleService) =>
        _helpArticleService = helpArticleService;

    [HttpGet]
    public IActionResult GetAllArticles()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return Ok(_helpArticleService.GetArticlesForPortal(role));
    }

    [HttpGet("{id}")]
    public IActionResult GetArticle(int id)
    {
        var article = _helpArticleService.GetArticleById(id);
        if (article is null) return NotFound("Article not found.");
        return Ok(article);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public IActionResult CreateArticle([FromBody] HelpArticle article)
    {
        if (article is null || string.IsNullOrWhiteSpace(article.Title))
            return BadRequest("Title is required.");

        _helpArticleService.SaveArticle(article);
        return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id}")]
    public IActionResult UpdateArticle(int id, [FromBody] HelpArticle article)
    {
        if (article is null || string.IsNullOrWhiteSpace(article.Title))
            return BadRequest("Title is required.");

        var existing = _helpArticleService.GetArticleById(id);
        if (existing is null) return NotFound("Article not found.");

        article.Id        = id;
        article.CreatedAt = existing.CreatedAt;
        _helpArticleService.SaveArticle(article);
        return Ok(article);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id}")]
    public IActionResult DeleteArticle(int id)
    {
        if (_helpArticleService.GetArticleById(id) is null)
            return NotFound("Article not found.");

        _helpArticleService.DeleteArticle(id);
        return NoContent();
    }
}
