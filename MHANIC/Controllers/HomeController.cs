
using MHANIC.Data;
using MHANIC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Net;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MHANIC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NicDbContext _context;
        //To Folder
        private readonly string _uploadsFolder = "C:\\FTP";
        // To FTP server 
        private readonly string ftpServer = "ftp://192.168.0.1/";
        private readonly string ftpUsername = "admin";
        private readonly string ftpPassword = "adP";

        private const string Username = "admin";
        private const string HashedPassword = "password";

        public HomeController(ILogger<HomeController> logger, NicDbContext db)
        {
            _context = db;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Delete(Int32? phoneNo)
        {
            if (phoneNo == null)
            {
                return NotFound();
            }

            var user = _context.UsersData.Find(phoneNo);

            // Check if the user record exists
            if (user == null)
            {
                return NotFound();
            }

            
            _context.UsersData.Remove(user);
            _context.SaveChanges(); 
            TempData["Message"] = "User record deleted successfully!";
            return RedirectToAction("Index"); 
           
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string captcha)
        {
            if (!ValidateCaptcha(captcha))
            {
                ViewBag.Error = "Invalid CAPTCHA code";
                return View();
            }

            if (username == Username && VerifyPassword(password, HashedPassword))
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }
        }

        public IActionResult Index()
        {
            IEnumerable<UserData> objList = _context.UsersData;
            return View(objList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(UserData userData, IFormFile PhotoData)
        {
            if (PhotoData != null && PhotoData.Length > 0)
            {
                var fileName = GenerateRandomFileName();
                var extension = Path.GetExtension(PhotoData.FileName);
                fileName = Path.ChangeExtension(fileName, extension);
                UploadFileToFTP(PhotoData, fileName);
                userData.PhotoName = fileName;
            }

            if (userData != null)
            {
                _context.UsersData.Add(userData);
                _context.SaveChanges();
                TempData["Message"] = "Form data submitted successfully!";
                return RedirectToAction("Index");
            }

            return View(userData);
        }

        public void UploadToFolder(string fileName, IFormFile PhotoData)
        {
            var filePath = Path.Combine(_uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                PhotoData.CopyTo(stream);
            }
        }

        [HttpGet]
        public IActionResult GetPhotoFromFolder(string fileName)
        {
            if(fileName == null)
            {
                return NotFound();
            }
            var filePath = Path.Combine(_uploadsFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                return PhysicalFile(filePath, "image/jpeg");
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult Search(Int32 phone)
        {
            var user = _context.UsersData.Find(phone);

            if (user == null)
            {
               
                ViewBag.Message = "No results found.";
                return RedirectToAction("Index");
            }
         
            return View(user);
        }
       
        private void UploadFileToFTP(IFormFile photo,string fileName)
        {
         
            string ftpFilePath = "/G/Shared/ftc/" + fileName;

            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpServer + ftpFilePath);
            ftpRequest.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            try
            {
                using (Stream fileStream = photo.OpenReadStream())
                using (Stream ftpStream = ftpRequest.GetRequestStream())
                {
                    fileStream.CopyTo(ftpStream);
                }

                Console.WriteLine("File uploaded successfully to FTP server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading file to FTP server: " + ex.Message);
            }
        }

        [HttpGet]
        public IActionResult DownloadPhoto(string fileName)
        {
            byte[] fileBytes = DownloadFromFTP(fileName);

            if (fileBytes != null)
            {
                // Return file as an image/jpeg file download
                return File(fileBytes, "image/jpeg", fileName);
            }
            else
            {
                TempData["Message"] = "File not found on FTP server.";
                return RedirectToAction("Error");
            }
        }

        private byte[] DownloadFromFTP(string fileName)
        {
            byte[] fileBytes = null;

            try
            {
            
                string ftpFilePath = "/G/Shared/ftc/" + fileName;

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + ftpFilePath);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    responseStream.CopyTo(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (Stream errorResponse = ex.Response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(errorResponse))
                    {
                        string error = reader.ReadToEnd();
                        Console.WriteLine("FTP Error: " + error);
                    }
                }
            }

            return fileBytes;
        }
        public IActionResult Privacy()
        {
            return View();
        }
        private bool VerifyPassword(string password, string hashedPassword)
        {
            using (var sha512 = System.Security.Cryptography.SHA512.Create())
            {
                byte[] hashedInputBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] hashedInputBytesStoredPassword = sha512.ComputeHash(Encoding.UTF8.GetBytes(HashedPassword));
                return Convert.ToBase64String(hashedInputBytes) == Convert.ToBase64String(hashedInputBytesStoredPassword);
            }
        }
        public IActionResult GenerateCaptcha()
        {
            string captchaCode = GenerateCaptchaCode(6); // Generate a 6-character CAPTCHA code
            HttpContext.Session.SetString("CaptchaCode", captchaCode); // Store the CAPTCHA code in session

            // Create a bitmap image and draw the CAPTCHA code
            using (var bitmap = new Bitmap(200, 50))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.DrawString(captchaCode, new Font("Arial", 20), Brushes.Black, new PointF(10, 10));

                // Convert the bitmap image to a byte array and return it as a file result
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }
        private string GenerateCaptchaCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private bool ValidateCaptcha(string enteredCaptcha)
        {
            string storedCaptcha = HttpContext.Session.GetString("CaptchaCode");
            return string.Equals(enteredCaptcha, storedCaptcha, StringComparison.OrdinalIgnoreCase);
        }
        private string GenerateRandomFileName()
        {
            // Generate a random 16-digit string
            var sb = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(random.Next(10));
            }
            return sb.ToString();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}