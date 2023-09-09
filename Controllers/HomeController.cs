using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyBlog.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace MyBlog.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BlogContext _context;

        public HomeController(ILogger<HomeController> logger, BlogContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {
            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
            {
                var author = _context.Author.FirstOrDefault(x => x.Email == Email && x.Password == Password);
                if (author == null)
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre");
                    return View();
                }

                // Kullanıcı kimliğini oturumda sakla
                HttpContext.Session.SetInt32("id", author.Id);

                // Kullanıcı kimliğini MyBlogs metoduna gönder
                return RedirectToAction(nameof(MyBlogs));
            }

            ModelState.AddModelError(string.Empty, "Kullanıcı adı ve şifre gereklidir.");
            return View();
        }

        public IActionResult MyBlogs(int id)
        {
            // Kategorileri veritabanından alın veya başka bir kaynaktan getirin
            var categories = _context.Categories.Select(w =>
                new SelectListItem
                {
                    Text = w.Name,
                    Value = w.Id.ToString()
                }
            ).ToList();

            int AutId = (int)HttpContext.Session.GetInt32("id");
            var list = _context.Blogs.Where(b => b.AuthorId == AutId).OrderByDescending(x => x.CreateTime).ToList();
            foreach (var blog in list)
            {
                blog.Author = _context.Author.Find(blog.AuthorId);
            }

            // ViewBag.Categories'a SelectListItem listesini ekleyin
            ViewBag.Categories = categories;

            return View(list);
        }
        

        public IActionResult AddBlog()
        {
            ViewBag.Categories = _context.Categories.Select(w =>
                new SelectListItem
                {
                    Text = w.Name,
                    Value = w.Id.ToString()
                }
            ).ToList();
            return View();
        }
        public async Task<IActionResult> Save(Blog model)
        {
            if (model != null)
            {
                var file = Request.Form.Files.First();
                //Blog için eklenen resimlerin kaydedilmesi istedinilen yer.
                string savePath = Path.Combine("C:", "Users", "cls", "Desktop", "BlogDeneme", "MyBlog", "wwwroot", "img");
                var fileName = $"{DateTime.Now:MMddHHmmss}.{file.FileName.Split(".").Last()}";
                var fileUrl = Path.Combine(savePath, fileName);
                using (var fileStream = new FileStream(fileUrl, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                model.ImagePath = fileName;
                model.AuthorId = (int)HttpContext.Session.GetInt32("id");
                await _context.AddAsync(model);
                await _context.SaveChangesAsync();
                return Json(true);
            }
            return Json(false);
        }

        public async Task<IActionResult> BlogDetails(int Id)
        {
            var blog = await _context.Blogs.FindAsync(Id);
            return Json(blog);
        }
        public async Task<IActionResult> UpdateBlog(Blog blog)
        {
            
                // Veritabanından güncellenen blogu alın
                var existingBlog = await _context.Blogs.FindAsync(blog.Id);

                if (existingBlog == null)
                {
                    return NotFound(); // Blog bulunamadıysa hata döndürün veya farklı bir işlem yapın
                }

                // Güncellenen verileri mevcut blog nesnesine kopyalayın
                existingBlog.Title = blog.Title;
                existingBlog.Subtitle = blog.Subtitle;
                existingBlog.Content = blog.Content;
                existingBlog.CategoryId = blog.CategoryId;

                // Diğer güncelleme işlemlerini burada yapabilirsiniz

                _context.Update(existingBlog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyBlogs)); // İşlem başarılıysa ana sayfaya yönlendirin
          

        
        }
        public async Task<IActionResult> DeleteBlog(int? Id)
        {
            Blog blog = await _context.Blogs.FindAsync(Id);
            _context.Remove(blog);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyBlogs));
        }
        public IActionResult Publish(int Id)
        {
            var blog = _context.Blogs.Find(Id);
            blog.IsPublish = true;
            _context.Update(blog);
            _context.SaveChanges();
            return RedirectToAction(nameof(MyBlogs));
        }




        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Index()
        {
            var list = _context.Blogs.Where(b => b.IsPublish).OrderByDescending(x => x.CreateTime).ToList();
            foreach (var blog in list)
            {
                blog.Author = _context.Author.Find(blog.AuthorId);
            }
            return View(list);
        }
        [HttpPost]
        public IActionResult AddComment(int id, string authorName, string commentText)
        {
            var blog = _context.Blogs.FirstOrDefault(b => b.Id == id);

            if (blog == null)
            {
                return NotFound(); // Blog bulunamadıysa 404 hatası döndür
            }

            if (blog.Comments == null)
            {
                blog.Comments = new List<Comment>(); // Yorumlar için bir liste oluşturun
            }

            if (!string.IsNullOrEmpty(authorName) && !string.IsNullOrEmpty(commentText))
            {
                // Yeni bir yorum oluşturun
                var newComment = new Comment
                {
                    AuthorName = authorName,
                    Text = commentText
                };

                // Blog ile yorumu ilişkilendirin
                blog.Comments.Add(newComment);

                // Veritabanına kaydedin
                _context.SaveChanges();
            }

            // Yorum ekledikten sonra, blog görüntüleme sayfasına yönlendirin
            return RedirectToAction("Post", new { id = id });
        }



        public IActionResult Post(int id)
        {
            var blog = _context.Blogs
                .Include(b => b.Comments) // Yorumları da getir
                .FirstOrDefault(b => b.Id == id);

            if (blog == null)
            {
                return NotFound(); // Blog bulunamadıysa 404 hatası döndür
            }

            blog.Author = _context.Author.Find(blog.AuthorId);
            blog.ImagePath = "/img/" + blog.ImagePath;
            return View(blog);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

}