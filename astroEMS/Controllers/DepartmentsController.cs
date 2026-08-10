using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Models;
using astroEMS.Data;
using Microsoft.AspNetCore.Authorization;

namespace astroEMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DEPARTMENTS
        public async Task<IActionResult> Index()
        {
            return View(await _context.Departments.ToListAsync());
        }

        // GET: DEPARTMENTS/Details/5
        public async Task<IActionResult> Details(int? departmentid)
        {
            if (departmentid == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.DepartmentID == departmentid);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: DEPARTMENTS/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DEPARTMENTS/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentID,Name")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: DEPARTMENTS/Edit/5
        public async Task<IActionResult> Edit(int? departmentid)
        {
            if (departmentid == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(departmentid);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: DEPARTMENTS/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? departmentid, [Bind("DepartmentID,Name")] Department department)
        {
            if (departmentid != department.DepartmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: DEPARTMENTS/Delete/5
        public async Task<IActionResult> Delete(int? departmentid)
        {
            if (departmentid == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.DepartmentID == departmentid);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: DEPARTMENTS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? departmentid)
        {
            var department = await _context.Departments.FindAsync(departmentid);
            if (department != null)
            {
                _context.Departments.Remove(department);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int? departmentid)
        {
            return _context.Departments.Any(e => e.DepartmentID == departmentid);
        }
    }
}