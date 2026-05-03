using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly BrandingSettingsService _brandingSettingsService;
    private readonly HelpArticleService _helpArticleService;

    public SettingsController(BrandingSettingsService brandingSettingsService, HelpArticleService helpArticleService)
    {
        _brandingSettingsService = brandingSettingsService;
        _helpArticleService = helpArticleService;
    }

    [HttpGet("branding")]
    public IActionResult GetBranding()
    {
        return Ok(_brandingSettingsService.GetSettings());
    }

    [Authorize]
    [HttpPost("branding")]
    public IActionResult UpdateBranding([FromBody] BrandingSettings settings)
    {
        if (settings is null)
            return BadRequest("Branding settings are required.");

        var updated = _brandingSettingsService.UpdateSettings(settings);
        return Ok(updated);
    }

    [HttpPost("help-articles")]
    public IActionResult SaveHelpArticle([FromBody] HelpArticle article)
    {
        if (article == null || string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
        {
            return BadRequest("Article title and content are required.");
        }

        _helpArticleService.SaveArticle(article);

        return Ok("Article saved successfully.");
    }

    [HttpGet("help-articles")]
    public IActionResult GetHelpArticles()
    {
        var articles = _helpArticleService.GetAllArticles();
        return Ok(articles);
    }
}
