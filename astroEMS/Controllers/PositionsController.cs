using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using astroEMS.Models;
using astroEMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace astroEMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class PositionsController : Controller
    {
        private readonly AppDbContext _context;

        public PositionsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Positions.ToListAsync());
        }

        public async Task<IActionResult> Details(int? positionid)
        {
            if (positionid == null) return NotFound();
            var position = await _context.Positions.FirstOrDefaultAsync(m => m.PositionID == positionid);
            if (position == null) return NotFound();
            return View(position);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PositionID,Name,DepartmentID")] Position position)
        {
            if (ModelState.IsValid)
            {
                _context.Add(position);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentID", "Name", position.DepartmentID);
            return View(position);
        }

        public async Task<IActionResult> Edit(int? positionid)
        {
            if (positionid == null) return NotFound();
            var position = await _context.Positions.FindAsync(positionid);
            if (position == null) return NotFound();
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentID", "Name", position.DepartmentID);
            return View(position);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? positionid, [Bind("PositionID,Name,DepartmentID")] Position position)
        {
            if (positionid != position.PositionID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(position);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PositionExists(position.PositionID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentID", "Name", position.DepartmentID);
            return View(position);
        }
        // GET: Positions/GetByDepartment/5 - used by JS to filter dropdown on Employee form
        [HttpGet]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var positions = await _context.Positions
                .Where(p => p.DepartmentID == departmentId)
                .OrderBy(p => p.Name)
                .Select(p => new { p.PositionID, p.Name })
                .ToListAsync();

            return Json(positions);
        }
        public async Task<IActionResult> Delete(int? positionid)
        {
            if (positionid == null) return NotFound();
            var position = await _context.Positions.FirstOrDefaultAsync(m => m.PositionID == positionid);
            if (position == null) return NotFound();
            return View(position);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? positionid)
        {
            var position = await _context.Positions.FindAsync(positionid);
            if (position != null) _context.Positions.Remove(position);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PositionExists(int? positionid)
        {
            return _context.Positions.Any(e => e.PositionID == positionid);
        }
    }
}