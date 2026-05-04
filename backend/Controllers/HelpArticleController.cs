using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpArticleController : ControllerBase
{
    private readonly HelpArticleService _helpArticleService;

    public HelpArticleController(HelpArticleService helpArticleService)
    {
        _helpArticleService = helpArticleService;
    }

    [HttpGet]
    public IActionResult GetAllArticles()
    {
        var articles = _helpArticleService.GetAllArticles();
        return Ok(articles);
    }

    [HttpGet("{id}")]
    public IActionResult GetArticle(int id)
    {
        var article = _helpArticleService.GetArticleById(id);
        
        if (article == null)
            return NotFound("Article not found.");

        return Ok(article);
    }

    [HttpPost]
    public IActionResult CreateArticle([FromBody] HelpArticle article)
    {
        if (article == null || string.IsNullOrWhiteSpace(article.Title))
            return BadRequest("Title is required.");

        _helpArticleService.SaveArticle(article);
        return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, article);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateArticle(int id, [FromBody] HelpArticle article)
    {
        if (article == null || string.IsNullOrWhiteSpace(article.Title))
            return BadRequest("Title is required.");

        var existing = _helpArticleService.GetArticleById(id);
        if (existing == null)
            return NotFound("Article not found.");

        article.Id = id;
        article.CreatedAt = existing.CreatedAt;
        _helpArticleService.SaveArticle(article);
        return Ok(article);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteArticle(int id)
    {
        var article = _helpArticleService.GetArticleById(id);
        if (article == null)
            return NotFound("Article not found.");

        _helpArticleService.DeleteArticle(id);
        return NoContent();
    }
}
