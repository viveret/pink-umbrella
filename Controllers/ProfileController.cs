using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using seattle.Models;
using seattle.Services;
using seattle.ViewModels.Profile;

namespace seattle.Controllers
{
    public class ProfileController: BaseController
    {
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IWebHostEnvironment environment, ILogger<ProfileController> logger, SignInManager<UserProfileModel> signInManager,
            UserManager<UserProfileModel> userManager, IPostService posts, IUserProfileService userProfiles):
            base(environment, signInManager, userManager, posts, userProfiles)
        {
            _logger = logger;
        }

        [Route("/Profile")]
        public async Task<IActionResult> Index() => RedirectToAction(nameof(Index), new { id = (await GetCurrentUserAsync()).Id });

        [Route("/Profile/{id}")]
        public async Task<IActionResult> Index(int id)
        {
            ViewData["Controller"] = "Profile";
            ViewData["Action"] = nameof(Index);
            var user = await _userProfiles.GetUser(id);
            return View(new IndexViewModel() {
                Profile = user,
                Feed = await _posts.GetPostsForUser(user.Id, user.Id, false, new PaginationModel() { count = 10, start = 0 })
            });
        }
        
        [Route("/Profile/{id}/Replies")]
        public async Task<IActionResult> Replies(int id)
        {
            ViewData["Controller"] = "Profile";
            ViewData["Action"] = nameof(Replies);
            var user = await _userProfiles.GetUser(id);
            return View("Index", new IndexViewModel() {
                Profile = user,
                Feed = await _posts.GetPostsForUser(user.Id, user.Id, true, new PaginationModel() { count = 10, start = 0 })
            });
        }
        
        [Route("/Profile/{id}/Mentions")]
        public async Task<IActionResult> Mentions(int id)
        {
            ViewData["Controller"] = "Profile";
            ViewData["Action"] = nameof(Mentions);
            var user = await _userProfiles.GetUser(id);
            return View("Index", new IndexViewModel() {
                Profile = user,
                Feed = await _posts.GetMentionsForUser(user.Id, user.Id, false, new PaginationModel() { count = 10, start = 0 })
            });
        }

        public async Task<IActionResult> ProfileByHandle(string handle)
        {
            var user = await _userProfiles.GetUser(handle);
            return RedirectToAction(nameof(Index), new { Id = user.Id });
        }
    }
}