using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

public class BlogController : Controller
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [Route("tin-tuc")]
    public async Task<IActionResult> Index()
    {
        var posts = await _blogService.GetPublishedPostsAsync();
        return View(posts);
    }

    // This action matches the Route in Program.cs: `pattern: "tin-tuc/{slug}"`
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _blogService.GetPostBySlugAsync(slug);
        
        // Ensure the post is published or user is Admin
        if (post == null || (!post.IsPublished && !User.IsInRole("Admin"))) return NotFound();

        return View(post);
    }
}
