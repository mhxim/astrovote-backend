﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using backend.Resources;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/v1/votes")]
    [ApiController]
    public class VotesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VotesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Votes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vote>>> GetVote()
        {
            return await _context.Vote.ToListAsync();
        }

        // GET: api/Votes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vote>> GetVote(Guid id)
        {
            var vote = await _context.Vote.FindAsync(id);

            if (vote == null)
            {
                return NotFound();
            }

            return vote;
        }

        // PUT: api/Votes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{postId}")]
        public async Task<IActionResult> PutVote(Guid postId, [FromBody] VoteResource voteResource)
        {
            var userName = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

            var post = await _context.Post.FindAsync(postId);

            var vote = await _context.Vote.Where(vote => vote.UserFK == userName && vote.PostFK == postId).FirstOrDefaultAsync();

            if (vote == null || post == null)
            {
                return NotFound();
            }

            post.Rating += voteResource.Rating - vote.Rating;
            vote.Rating = voteResource.Rating;
            vote.Updated = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VoteExists(vote.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { Rating = post.Rating });
        }

        // POST: api/Votes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Vote>> PostVote([FromBody]VoteResource voteResource)
        {
            var userName = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            var dbVote = await _context.Vote.Where(vote => vote.UserFK == userName && vote.PostFK == voteResource.PostFK).FirstOrDefaultAsync();

            if (dbVote != null)
            {
                return BadRequest();
            }
         
            var post = await _context.Post.FindAsync(voteResource.PostFK);
            post.Rating += voteResource.Rating - 3;

            var vote = new Vote()
            {
                PostFK = voteResource.PostFK,
                Rating = voteResource.Rating,
                UserFK = userName,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            _context.Vote.Add(vote);
            await _context.SaveChangesAsync();

            return Ok(new { Rating = post.Rating });
        }

        // DELETE: api/Votes/5
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeleteVote(Guid postId)
        {
            var userName = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            var vote = await _context.Vote.Where(vote => vote.UserFK == userName && vote.PostFK == postId).FirstOrDefaultAsync();

            if (vote == null)
            {
                return NotFound();
            }

            _context.Vote.Remove(vote);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VoteExists(Guid id)
        {
            return _context.Vote.Any(e => e.Id == id);
        }
    }
}
