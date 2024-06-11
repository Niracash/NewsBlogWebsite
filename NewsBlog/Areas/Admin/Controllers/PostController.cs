﻿using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsBlog.Data;
using NewsBlog.Models;
using NewsBlog.Utilities;
using NewsBlog.ViewModels;
using System.Configuration;
using X.PagedList;

namespace NewsBlog.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PostController : Controller
    {
        private readonly AppDbContext _db;
        private readonly INotyfService _notification;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        public PostController(AppDbContext db, INotyfService notyfService, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _db = db;
            _notification = notyfService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? page)
        {
            var postList = new List<Post>();
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var userRole = await _userManager.GetRolesAsync(user!);
            if (userRole[0] == Roles.Admin)
            {
                postList = await _db.Posts!.Include(x => x.User).ToListAsync();
            }
            else
            {
                postList = await _db.Posts!.Include(x => x.User).Where(x => x.User!.Id==user!.Id).ToListAsync();
            }
            var PostListViewModel = postList.Select(x => new PostListViewModel()
            {
                Id = x.Id,
                ImageUrl = x.ImageUrl,
                Title = x.Title,
                Description = x.Description,
                AuthorName = x.User!.FirstName + " " + x.User!.LastName,
                CreatedAt = x.CreatedAt                
            }).ToList();
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(await PostListViewModel.OrderByDescending(x => x.CreatedAt).ToPagedListAsync(pageNumber, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.Posts!.FirstOrDefaultAsync(x=>x.Id == id);
            if(post == null)
            {
                _notification.Error("Post not found");
                return View();
            }
            // logged in user
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var userRole = await _userManager.GetRolesAsync(user!);
            if (userRole[0] != Roles.Admin && user!.Id != post.UserId)
            {
                _notification.Error("This is not your post!");
                return RedirectToAction("Index");
            }

            var viewModel = new CreatePostViewModel()
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                ImageUrl = post.ImageUrl,
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreatePostViewModel createPostViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(createPostViewModel);
            }
            var post = await _db.Posts!.FirstOrDefaultAsync(x=> x.Id == createPostViewModel.Id);
            if(post == null)
            {
                _notification.Error("Post not found");
                return View();
            }
            post.Title = createPostViewModel.Title;
            post.Description = createPostViewModel.Description;

            if (createPostViewModel.UploadImage != null)
            {
                post.ImageUrl = Image(createPostViewModel.UploadImage);
            }
            await _db.SaveChangesAsync();
            _notification.Success("Post updated!");
            return RedirectToAction("Index", "Post", new { area = "Admin" });


        }
        [HttpPost]
        public async Task<IActionResult> Create(CreatePostViewModel createPostViewModel)
        {
            if(!ModelState.IsValid)
            {
                return View(createPostViewModel);
            }

            //get logged in user's id
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);


            var post = new Post();
            post.Title = createPostViewModel.Title;
            post.Description = createPostViewModel.Description;
            post.UserId = user!.Id;

            if(post.Title != null)
            {
                string slug = createPostViewModel.Title!.Trim();
                slug = slug.Replace(" ", "-");
                post.Slug = slug +  "-" + Guid.NewGuid();
            }

            if(createPostViewModel.UploadImage != null)
            {
                post.ImageUrl = Image(createPostViewModel.UploadImage);
            }

            await _db.Posts!.AddAsync(post);
            await _db.SaveChangesAsync();
            _notification.Success("Blog created successfully");

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var autherPost = await _db.Posts!.FirstOrDefaultAsync(x => x.Id == id);
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == User.Identity!.Name);
            var userRole = await _userManager.GetRolesAsync(user!);
            if (userRole[0] == Roles.Admin || user?.Id == autherPost?.UserId)
            {

                _db.Posts!.Remove(autherPost!);
                await _db.SaveChangesAsync();
                _notification.Success("Post have been deleted");
                return RedirectToAction("Index", "Post", new { area = "Admin" });
            }
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreatePostViewModel());
        }

        private string Image(IFormFile file)
        {
            string uniqueFileName = "";
            var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            uniqueFileName = Guid.NewGuid().ToString()+ "_" + file.FileName;
            var filePath = Path.Combine(folderPath, uniqueFileName);
            using(FileStream fileStream = System.IO.File.OpenWrite(filePath))
            {
                file.CopyTo(fileStream);
            }
            return uniqueFileName;
        }
    }
}
