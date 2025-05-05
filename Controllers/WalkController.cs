using DogTracker.Interfaces;
using DogTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace DogTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalkController(IWalkService walkService) : ControllerBase
{
    [HttpGet("recent/{dogId}")]
    public async Task<IActionResult> GetRecentWalks(int dogId)
    {
        var walks = await walkService.GetRecentWalksAsync(dogId);
        return Ok(walks);
    }

    [HttpPost("add/{dogId}")]
    public async Task<IActionResult> AddWalk(int dogId, [FromBody] Walk? walk)
    {
        if (walk == null)
            return BadRequest("Invalid walk data.");

        await walkService.AddWalkAsync(dogId, walk);
        return Ok();
    }

    // [HttpDelete("delete/{dogId}/{walkId}")]
    // public async Task<IActionResult> DeleteWalk(int dogId, int walkId)
    // {
    //     await walkService.DeleteWalkAsync(dogId, walkId);
    //     return Ok();
    // }

    [HttpGet("{walkId}")]
    public async Task<IActionResult> GetWalkById(int walkId)
    {
        var walk = await walkService.GetWalkByIdAsync(walkId);
        if (walk == null)
            return NotFound();

        return Ok(walk);
    }
}