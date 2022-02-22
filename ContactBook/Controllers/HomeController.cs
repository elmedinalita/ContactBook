using ContactBook.Data;
using ContactBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Data;
using System.Net.Http.Headers;

namespace ContactBook.Controllers
{
    public class HomeController : Controller
    {
        readonly ApplicationDbContext _db;
        readonly IWebHostEnvironment _env;
        string _filePath { get { return string.Format("{0}/{1}", _env.WebRootPath, _fileName); } }
        static string _fileName = "Contacts.xlsx";

        #region General
        public HomeController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var _contacts = await _db.Contacts.AsNoTracking().Include(c => c.Email).Include(c => c.Phone).Include(c => c.Address)
                .Where(c => c.FirstName.Contains(id) ||
                            c.LastName.Contains(id) ||
                            (c.Email.Where(e => e.EmailAddr.Contains(id)).ToArray().Count() > 0) ||
                            (c.Phone.Where(p => p.PhoneNo.Contains(id)).ToArray().Count() > 0) ||
                            (c.Address.Where(a => a.Addr.Contains(id)).ToArray().Count() > 0)).ToListAsync();
                return View(_contacts);
            }
            var contacts = await _db.Contacts.Include(c => c.Email).Include(c => c.Phone).Include(c => c.Address).AsNoTracking().ToListAsync();
            return View(contacts);
            // hey.
        }

        //[Authorize(Roles = "RW")]
        public async Task<IActionResult> CreateAsync(int? id)
        {
            if (id == null)
            {
                var cont = new Contact();
                cont.Email.Add(new Email());
                cont.Phone.Add(new Phone());
                cont.Address.Add(new Address());
                return View(cont);
            }
            var con = await _db.Contacts.AsNoTracking().Include(c => c.Email).Include(c => c.Phone).Include(c => c.Address).FirstOrDefaultAsync(c=> c.Id == id);
            return View(con);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contact contact)
        {
            if (!ModelState.IsValid)
                return View(contact);

            var _cont = _db.Contacts.Include(c => c.Email).Include(c=> c.Phone).Include(c => c.Address).FirstOrDefault(c => c.Id == contact.Id);
            if (_cont == null)
                await _db.Contacts.AddAsync(contact);
            else
            {
                _cont.FirstName = contact.FirstName;
                _cont.LastName = contact.LastName;
                _cont.Email = contact.Email;
                _cont.Phone = contact.Phone;
                _cont.Address = contact.Address;
                _db.Contacts.Update(_cont);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        //[Authorize(Roles = "RW")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var obj = await _db.Contacts.FindAsync(id);
            if (obj == null)
                return NotFound();
            return View(obj);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Contact contact)
        {
            if (ModelState.IsValid)
            {
                // Need to get rid of the children as well...
                var c = await _db.Contacts.Include(c => c.Email).Include(c => c.Phone).Include(c => c.Address).FirstOrDefaultAsync(con => con.Id == contact.Id);
                if (c != null)
                {
                    _db.Contacts.Remove(c);
                    await _db.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }
            else return View();
        }
        #endregion

        #region Import / Export

        //[Authorize(Roles = "RW")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(IList<IFormFile> files)
        {
            var status = "";
            foreach (IFormFile source in files)
            {
                string filename = ContentDispositionHeaderValue.Parse(source.ContentDisposition).FileName!.Trim('"');
                filename = Path.Combine(_env.WebRootPath, EnsureCorrectFilename(filename));
                try
                {
                    using FileStream output = System.IO.File.Create(filename);
                    await source.CopyToAsync(output);

                    // Read the data
                    if (Path.GetExtension(filename).ToLower() == "csv")
                    {
                        var text = System.IO.File.ReadAllText(filename);
                        var contacts = Helper.ImportCSV(ref text);
                        await _db.Contacts.AddRangeAsync(contacts);
                    }
                    else if (Path.GetExtension(filename).ToLower() == "csv")
                    {
                        var contacts = Helper.ImportExcel(_filePath);
                        await _db.Contacts.AddRangeAsync(contacts);
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message;
                }
                finally
                {
                    System.IO.File.Delete(filename);
                }
            }
            return Json("status", status);
        }
        string EnsureCorrectFilename(string filename)
        {
            if (filename.Contains("\\"))
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);

            return filename;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportCSV([FromBody] Contact[] contacts)
        {
            var errorMessage = "";
            _fileName = "Contacts.csv";
            try
            {
                var data = Helper.ExportCSV(ref contacts);
                System.IO.File.WriteAllText(_filePath, data);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            return Json(new { _fileName, errorMessage });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportExcel([FromBody] Contact[] contacts)
        {
            var errorMessage = "";
            _fileName = "Contacts.xlsx";
            try
            {
                Helper.ExportExcel(_filePath, ref contacts);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            
            return Json(new { _fileName, errorMessage });
        }

        [HttpGet]
        public ActionResult Download()
        {
            byte[] fileByteArray = System.IO.File.ReadAllBytes(_filePath);
            System.IO.File.Delete(_filePath);
            return File(fileByteArray, "application/excel", _fileName);
        }

        #endregion

        #region HomeViews
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        #endregion
    }
}