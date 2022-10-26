using EntityFrameworkProject.Data;
using EntityFrameworkProject.Helpers;
using EntityFrameworkProject.Models;
using EntityFrameworkProject.ViewModels.ProductViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkProject.Areas.AdminArea.Controllers
{
    [Area("AdminArea")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        public async Task<IActionResult> Index(int page = 1, int take = 5)
        {
            List<Product> products = await _context.Products
                .Where(m => !m.IsDeleted)
                .Include(m => m.ProductImages)
                .Include(m => m.Category)
                .Skip((page*take)-take)
                .Take(take)
                .OrderByDescending(m => m.Id)   
                .ToListAsync();

            List<ProductListVM> mapDatas = GetMapDatas(products);

            int count = await GetPageCount(take);

            Paginate<ProductListVM> result = new Paginate<ProductListVM>(mapDatas, page, count);
            return View(result);
        }

        public async Task<int> GetPageCount(int take)
        {
            int productCount = await _context.Products.Where(m =>!m.IsDeleted).CountAsync();

            return (int)Math.Ceiling((decimal)productCount / take);
        }

        private List<ProductListVM> GetMapDatas(List<Product> products)
        {
            List<ProductListVM> productList = new List<ProductListVM>();
            foreach (var product in products)
            {
                ProductListVM newProduct = new ProductListVM {
                    Id = product.Id,
                    Title = product.Title,
                    Description = product.Description,
                    MainImage = product.ProductImages.Where(m=>m.IsMain).FirstOrDefault()?.Image,
                    CategoryName = product.Category.Name,
                    Price = product.Price                
                };
                productList.Add(newProduct);
            }
            return productList;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.categories = await GetCategories(); 

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateVM product)
        {
            ViewBag.categories = await GetCategories();
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            foreach (var photo in product.Photos)
            {
                if (!photo.CheckFileType("image/"))
                {
                    ModelState.AddModelError("Photo", "Please choose correct image type");
                    return View(product);
                }

                if (!photo.CheckFileSize(500))
                {
                    ModelState.AddModelError("Photo", "Please choose correct image size");
                    return View(product);
                }
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {

            Product products = await _context.Products.FirstOrDefaultAsync();

            ProductCreateVM product = new ProductCreateVM() { 
            Title = products.Title
            };

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.categories = await GetCategories();
            Product product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            ProductCreateVM productCreateVM = new ProductCreateVM() { 
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId
            };

            return View(productCreateVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateVM productCreateVM)
        {
            Product product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            ProductCreateVM dbProductCreateVM = new ProductCreateVM()
            {
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
             
                
            };

            //dbProductCreateVM.Title = productCreateVM.Title;

            //dbProductCreateVM.Description = productCreateVM.Description;

            //dbProductCreateVM.Price = productCreateVM.Price;

        

            List<ProductImage> images = new List<ProductImage>();

            foreach (var photo in productCreateVM.Photos)
            {
                string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName;

                string path = Helper.GetFilePath(_env.WebRootPath, "img", fileName);

                await Helper.SaveFile(path, photo);


                ProductImage image = new ProductImage
                {
                    Image = fileName,
                };

                images.Add(image);
            }

            images.FirstOrDefault().IsMain = true;

            ProductCreateVM dbProductCreate = new ProductCreateVM
            {
                Title = productCreateVM.Title,
                Description = productCreateVM.Description,            
                Price = productCreateVM.Price,            
                
            };


            product.Title = productCreateVM.Title;

            product.Description = productCreateVM.Description;

            product.Price = productCreateVM.Price;


            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }


        private async Task<SelectList> GetCategories()
        {
            IEnumerable<Category> categories = await _context.Categories.Where(m => !m.IsDeleted).ToListAsync();
            return new SelectList(categories, "Id", "Name");
        }
    }
}
