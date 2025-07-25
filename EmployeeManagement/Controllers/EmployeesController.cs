using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Models;

namespace EmployeeManagement.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly EmployeeDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EmployeesController(EmployeeDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchTerm)
        {
            var employees = _context.Employees.Include(e => e.Department).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                employees = employees.Where(e =>
                    (!string.IsNullOrEmpty(e.EmployeeName) && e.EmployeeName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(e.Phone) && e.Phone.ToLower().Contains(searchTerm)));
            }

            return View(await employees.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                if (employee.Photo != null)
                {
                    try
                    {
                        employee.PhotoImagePath = await SavePhotoAsync(employee.Photo);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Photo", ex.Message);
                        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
                        return View(employee);
                    }
                }

                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            return View(employee);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldData = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id);

                    if (employee.Photo != null)
                    {
                        try
                        {
                            employee.PhotoImagePath = await SavePhotoAsync(employee.Photo);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("Photo", ex.Message);
                            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
                            return View(employee);
                        }
                    }
                    else
                    {
                        employee.PhotoImagePath = oldData?.PhotoImagePath;
                    }

                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            return View(employee);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            var extension = Path.GetExtension(photo.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only .jpg, .jpeg, .png files are allowed.");

            if (photo.Length > 2 * 1024 * 1024)
                throw new InvalidOperationException("Max file size is 2MB.");

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/photos");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return "/images/photos/" + uniqueFileName;
        }

        public IActionResult Statistics()
        {
            var stats = _context.Departments
                .Select(d => new EmployeeStatisticsViewModel
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    TotalEmployees = d.Employees.Count(),
                    TotalMale = d.Employees.Count(e => e.Gender == true),
                    TotalFemale = d.Employees.Count(e => e.Gender == false)
                })
                .ToList();

            return View(stats);
        }
    }
}