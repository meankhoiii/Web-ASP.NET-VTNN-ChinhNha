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
        ViewData["MetaDescription"] = "Tin tuc ky thuat mua vu, huong dan su dung phan bon va kinh nghiem canh tac tu ChinhNha.";
        var posts = await _blogService.GetPublishedPostsAsync();
        return View(posts);
    }

    // This action matches the Route in Program.cs: `pattern: "tin-tuc/{slug}"`
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _blogService.GetPostBySlugAsync(slug);
        
        // Ensure the post is published or user is Admin
        if (post == null || (!post.IsPublished && !User.IsInRole("Admin"))) return NotFound();

        ViewData["MetaDescription"] = post.MetaDescription ?? post.Summary ?? post.Title;
        ViewData["OgImage"] = post.FeaturedImageUrl;

        return View(post);
    }
}
