using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Presentation.Authorization;

[ApiController]
[Route("api/[controller]")]
public class ClubPostController : ControllerBase
{
    private readonly IClubPostService _clubPostService;
    private readonly IFileStorageService _fileStorageService;

    public ClubPostController(IClubPostService clubPostService, IFileStorageService fileStorageService)
    {
        _clubPostService = clubPostService;
        _fileStorageService = fileStorageService;
    }

    /// <summary>Get all club posts</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var posts = await _clubPostService.GetAllAsync();
        return Ok(new { success = true, data = posts });
    }

    /// <summary>Get all posts of a club</summary>
    [HttpGet("club/{clubId}")]
    public async Task<IActionResult> GetByClubId(int clubId)
    {
        var posts = await _clubPostService.GetByClubIdAsync(clubId);
        return Ok(new { success = true, data = posts });
    }

    /// <summary>Get all posts linked to an event</summary>
    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetByEventId(int eventId)
    {
        var posts = await _clubPostService.GetByEventIdAsync(eventId);
        return Ok(new { success = true, data = posts });
    }

    /// <summary>Get all posts linked to a recruitment campaign</summary>
    [HttpGet("campaign/{campaignId}")]
    public async Task<IActionResult> GetByCampaignId(int campaignId)
    {
        var posts = await _clubPostService.GetByCampaignIdAsync(campaignId);
        return Ok(new { success = true, data = posts });
    }

    /// <summary>Get club post by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var post = await _clubPostService.GetByIdAsync(id);
        if (post == null)
            return NotFound(new { success = false, message = "Club post not found" });

        return Ok(new { success = true, data = post });
    }

    /// <summary>Get posts by user ID</summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var posts = await _clubPostService.GetByUserIdAsync(userId);
        return Ok(new { success = true, data = posts });
    }

    /// <summary>Create a new club post</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequireClubPolicyOrRole("CreatePost", "Admin")]
    public async Task<IActionResult> Create([FromForm] CreateClubPostDto dto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

        try
        {
            var post = await _clubPostService.CreateAsync(dto, imageFile);
            return CreatedAtAction(nameof(GetById), new { id = post.PostId }, new
            {
                success = true,
                message = imageFile != null
                    ? "Club post created successfully. Image is being processed."
                    : "Club post created successfully",
                data = post
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Update a club post</summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequireClubPolicyOrRole("Club Manager", "EditPost")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateClubPostDto dto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

        try
        {
            var post = await _clubPostService.UpdateAsync(id, dto, imageFile);
            if (post == null)
                return NotFound(new { success = false, message = "Club post not found" });

            return Ok(new
            {
                success = true,
                message = imageFile != null
                    ? "Club post updated successfully. New image is being processed."
                    : "Club post updated successfully",
                data = post
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Soft-delete a club post</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _clubPostService.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "Club post not found" });

        return Ok(new { success = true, message = "Club post deleted successfully" });
    }

    /// <summary>Upload inline image for CKEditor5</summary>
    [HttpPost("upload-editor-image")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadEditorImage(IFormFile upload)
    {
        if (upload == null || upload.Length == 0)
            return BadRequest(new { error = new { message = "No file uploaded" } });

        try
        {
            var url = await _fileStorageService.SaveFileAsync(upload, "clubposts");
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = new { message = ex.Message } });
        }
    }
}
